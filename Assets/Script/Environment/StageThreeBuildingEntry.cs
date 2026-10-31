using UnityEngine;
public sealed class StageThreeBuildingEntry : MonoBehaviour
{
    void Start()
    {
        var point=new Vector3(-23f,0f,0f);
        var root=GameObject.Find("Unit000");var player=GameObject.FindGameObjectWithTag("Player");
        if(root!=null){root.transform.position=point;if(player!=null)player.transform.localPosition=Vector3.zero;}
        else if(player!=null)player.transform.position=point;
        var spawn=GameObject.FindGameObjectWithTag("Spawn");if(spawn!=null)spawn.transform.position=point;
    }
}
