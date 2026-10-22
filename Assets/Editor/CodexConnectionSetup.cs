using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

// One-time connection bootstrap. Does not modify scenes or runtime game code.
[InitializeOnLoad]
public static class CodexConnectionSetup
{
    const string Marker = "Library/CodexConnectionSetup.pending";
    const string Report = "Library/CodexConnectionSetup.log";
    const string Package = "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.3.0";
    static AddRequest request;
    static bool started;
    static CodexConnectionSetup() { EditorApplication.delayCall += Begin; }

    [MenuItem("Tools/Codex/Connect Unity MCP")]
    public static void ConnectFromMenu()
    {
        if (started)
        {
            Write("Connection setup is already running.");
            return;
        }
        Directory.CreateDirectory("Library");
        File.WriteAllText(Marker, string.Empty);
        SessionState.SetBool("CodexConnectionSetup.InstallRequested", false);
        Begin();
    }

    static void Write(string message)
    {
        File.AppendAllText(Report, DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
        Debug.Log("[CodexConnectionSetup] " + message);
    }

    static void Begin()
    {
        if (!File.Exists(Marker) || started) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Begin;
            return;
        }
        started = true;
        var locator = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("MCPForUnity.Editor.Services.MCPServiceLocator", false))
            .FirstOrDefault(t => t != null);
        if (locator != null) { Connect(locator); return; }
        if (SessionState.GetBool("CodexConnectionSetup.InstallRequested", false))
        {
            started = false;
            Write("Waiting for package compilation; installation already requested this session.");
            return;
        }
        SessionState.SetBool("CodexConnectionSetup.InstallRequested", true);
        Write("Installing MCP for Unity 10.3.0 via Unity Package Manager.");
        request = Client.Add(Package);
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (request == null || !request.IsCompleted) return;
        EditorApplication.update -= Poll;
        if (request.Status == StatusCode.Success)
        {
            Write("Package installed: " + request.Result.name + " " + request.Result.version);
            started = false;
            EditorApplication.delayCall += Begin;
        }
        else
        {
            started = false;
            Write("Package installation failed: " + request.Error.message);
        }
    }

    static async void Connect(Type locator)
    {
        try
        {
            var cacheType = locator.Assembly.GetType("MCPForUnity.Editor.Services.EditorConfigurationCache", true);
            var cache = cacheType.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            cacheType.GetMethod("SetUseHttpTransport").Invoke(cache, new object[] { true });
            cacheType.GetMethod("SetHttpTransportScope").Invoke(cache, new object[] { "local" });
            cacheType.GetMethod("SetHttpBaseUrl").Invoke(cache, new object[] { "http://127.0.0.1:8080" });

            // Let the package discover uv/uvx on this PC and respect its path override.
            var server = locator.GetProperty("Server").GetValue(null);
            var serverType = server.GetType();
            var reachable = serverType.GetMethod("IsLocalHttpServerReachable");
            if (!(bool)reachable.Invoke(server, null))
            {
                Write("Starting the local Unity MCP server.");
                bool launched = (bool)serverType.GetMethod("StartLocalHttpServer")
                    .Invoke(server, new object[] { true });
                if (!launched)
                    throw new InvalidOperationException("MCP server could not start. Check that uv/uvx is installed and inspect the Unity Console.");

                var deadline = DateTime.UtcNow.AddMinutes(5);
                while (!(bool)reachable.Invoke(server, null))
                {
                    if (DateTime.UtcNow >= deadline)
                        throw new TimeoutException("Unity MCP server did not become ready within five minutes.");
                    await Task.Delay(500);
                }
            }

            var bridge = locator.GetProperty("Bridge").GetValue(null);
            var task = (Task<bool>)bridge.GetType().GetMethod("StartAsync").Invoke(bridge, null);
            bool connected = await task;
            if (!connected)
                throw new InvalidOperationException("The Unity Editor could not connect to the local MCP server.");

            var verification = (Task)bridge.GetType().GetMethod("VerifyAsync").Invoke(bridge, null);
            await verification;
            var result = verification.GetType().GetProperty("Result").GetValue(verification);
            if (!(bool)result.GetType().GetProperty("Success").GetValue(result))
                throw new InvalidOperationException("Unity MCP bridge verification failed.");

            File.Delete(Marker);
            Write("Unity MCP bridge verified. On this PC run: codex mcp add unityMCP --url http://127.0.0.1:8080/mcp. Then restart Codex and open this project.");
        }
        catch (Exception e) { Write("Connection setup failed: " + e); }
        finally { started = false; }
    }
}
