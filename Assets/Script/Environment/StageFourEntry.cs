using UnityEngine;

// Shared by direct scene starts, portals and the respawn marker.
public sealed class StageFourEntry : MonoBehaviour
{
    public static readonly Vector3 StartPosition = new Vector3(-30.5f, 5.5f, 0f);

    void Start()
    {
        var root = GameObject.Find("Unit000");
        var player = GameObject.FindGameObjectWithTag("Player");
        if (root != null)
        {
            root.transform.position = StartPosition;
            if (player != null) player.transform.localPosition = Vector3.zero;
        }
        else if (player != null) player.transform.position = StartPosition;
        var spawn = GameObject.FindGameObjectWithTag("Spawn");
        if (spawn != null) spawn.transform.position = StartPosition;
    }
}
