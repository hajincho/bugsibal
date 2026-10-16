using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string next_stage;
    public GameObject player;
    public GameObject real_player;
    public int nextstage;
    public float x,y,z;
    bool entering;
    void Start(){player=GameObject.Find("Unit000");real_player=GameObject.FindWithTag("Player");}
    void OnTriggerEnter2D(Collider2D other)
    {if(other.CompareTag("Player"))Enter();}
    public void Enter()
    {
        if(entering || StageRewardController.InputBlocked || GameManager.player_current_HP<=0)return;
        if(!Application.CanStreamedLevelBeLoaded(next_stage)){Debug.LogError("Portal destination is unavailable: "+next_stage);return;}
        var controller=StageRewardController.Ensure();
        entering=true;
        // The persistent controller owns this callback while a reward is being selected.
        string destination=next_stage;int stageNumber=nextstage;Vector3 position=new Vector3(x,y,z);
        GameObject root=player,character=real_player;
        Action transition=()=>Transition(destination,stageNumber,position,root,character);
        if(controller!=null){if(!controller.RequestTransition(destination,transition))entering=false;}
        else transition();
    }
    static void Transition(string destination,int number,Vector3 position,GameObject root,GameObject character)
    {
        // A static closure survives destruction of the source-scene portal.
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> callback=null;
        callback=(scene,mode)=>
        {
            if(scene.name!=destination)return;
            SceneManager.sceneLoaded-=callback;
            if(root!=null)root.transform.position=position;
            if(character!=null)character.transform.localPosition=Vector3.zero;
            GameManager.current_stage=number;
        };
        SceneManager.sceneLoaded+=callback;
        try{SceneManager.LoadScene(destination);}
        catch(Exception e)
        {
            SceneManager.sceneLoaded-=callback;
            if(StageRewardController.Instance!=null)StageRewardController.Instance.TransitionFailed();
            Debug.LogException(e);
        }
    }
}
