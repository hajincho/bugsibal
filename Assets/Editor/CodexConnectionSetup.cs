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
        else Write("Package installation failed: " + request.Error.message);
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
            cacheType.GetMethod("SetUvxPathOverride").Invoke(cache, new object[] { @"C:\Users\srobo\Documents\Codex\2026-10-16\x20-1-2-3-4-5-5\work\uv-runtime\bin\uvx.exe" });
            var bridge = locator.GetProperty("Bridge").GetValue(null);
            var task = (Task<bool>)bridge.GetType().GetMethod("StartAsync").Invoke(bridge, null);
            bool connected = await task;
            Write("Bridge connection result: " + connected);
            if (connected) File.Delete(Marker);
        }
        catch (Exception e) { Write("Connection setup failed: " + e); }
    }
}
