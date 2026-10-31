using UnityEngine;

// Covers direct scene starts as well as normal portal transitions.
public sealed class StageTwoForestEntry : MonoBehaviour
{
    void Start()
    {
        var root=GameObject.Find("Unit000");
        var player=GameObject.FindGameObjectWithTag("Player");
        var point=new Vector3(-25f,8.5f,0);
        if(root!=null){root.transform.position=point;if(player!=null)player.transform.localPosition=Vector3.zero;}
        else if(player!=null)player.transform.position=point;
        var spawn=GameObject.FindGameObjectWithTag("Spawn");if(spawn!=null)spawn.transform.position=point;
    }
}
