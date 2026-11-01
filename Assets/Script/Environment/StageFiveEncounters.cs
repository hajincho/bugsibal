using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageFiveEncounters : MonoBehaviour
{
    [Serializable] public sealed class Zone
    {
        public string title;
        public Vector2 center;
        public float radius=8f;
        public GameObject[] enemies;
        public Vector3[] spawnPoints;
        [HideInInspector] public bool activated,cleared;
        [HideInInspector] public int spawned;
        [HideInInspector] public float readyAt;
        [HideInInspector] public List<EnemyData> living=new List<EnemyData>();
    }
    public Zone[] zones;
    public GameObject warningPrefab,exitPortal;
    public int maximumAlive=14;
    public UnityEngine.UI.Text remainingEnemiesText;
    public bool Completed { get; private set; }
    public int AliveCount
    {
        get { int count=0;foreach(var e in FindObjectsOfType<EnemyData>())if(e.gameObject.scene==gameObject.scene&&e.CompareTag("Enemy")&&e.enemy_current_HP>0f)count++;return count; }
    }
    Player player;
    void OnEnable()
    {
        // Keep the displayed stage correct after an editor script reload as well.
        if(Application.isPlaying)GameManager.current_stage=5;
    }
    void Start()
    {
        player=FindObjectOfType<Player>();
        var root=GameObject.Find("Unit000");if(root!=null)root.transform.position=new Vector3(0,-40,0);
        if(player!=null)player.transform.localPosition=Vector3.zero;
        GameManager.current_stage=5;
        if(zones.Length>0 && zones[0].enemies.Length==0)
        {
            zones[0].activated=true;
            zones[0].cleared=true;
        }
        if(exitPortal!=null)exitPortal.SetActive(false);
    }
    public bool CanActivate(int index)
    {
        if(index==0)return true;
        if(index==1||index==2)return zones[0].cleared;
        if(index==3)return zones[1].cleared||zones[2].cleared;
        if(index==4||index==5)return zones[3].cleared;
        return zones[4].cleared||zones[5].cleared;
    }
    public bool MainRouteCleared => zones[0].cleared&&(zones[1].cleared||zones[2].cleared)&&zones[3].cleared&&(zones[4].cleared||zones[5].cleared)&&zones[6].cleared;
    int Remaining(Zone zone)
    {
        if(zone.cleared)return 0;int count=Mathf.Max(0,zone.enemies.Length-zone.spawned);
        foreach(var enemy in zone.living)if(enemy!=null&&enemy.enemy_current_HP>0f)count++;
        return count;
    }
    int BranchRemaining(int a,int b)
    {
        if(zones[a].activated&&zones[b].activated)return Remaining(zones[a])+Remaining(zones[b]);
        if(zones[a].activated)return Remaining(zones[a]);if(zones[b].activated)return Remaining(zones[b]);
        return Mathf.Min(Remaining(zones[a]),Remaining(zones[b]));
    }
    public int RequiredRemaining
    {
        get
        {
            if(Completed)return 0;int assignedAlive=0;
            foreach(var zone in zones)foreach(var enemy in zone.living)if(enemy!=null&&enemy.enemy_current_HP>0f)assignedAlive++;
            return Remaining(zones[0])+Remaining(zones[3])+Remaining(zones[6])+BranchRemaining(1,2)+BranchRemaining(4,5)+Mathf.Max(0,AliveCount-assignedAlive);
        }
    }
    void LateUpdate()
    {
        if(remainingEnemiesText!=null)remainingEnemiesText.text="남은 적의 수: "+AliveCount+"\n클리어까지 남은 적: "+RequiredRemaining;
    }
    void Update()
    {
        if(Completed||StageRewardController.InputBlocked||GameManager.player_current_HP<=0f)return;
        if(player==null)player=FindObjectOfType<Player>();if(player==null)return;
        int alive=AliveCount;
        for(int i=0;i<zones.Length;i++)
        {
            var z=zones[i];z.living.RemoveAll(e=>e==null||e.enemy_current_HP<=0f);
            if(!z.activated&&CanActivate(i)&&Vector2.Distance(player.transform.position,z.center)<z.radius+2f)
            {
                z.activated=true;z.readyAt=Time.time+.8f;
                if(warningPrefab!=null)foreach(var point in z.spawnPoints){var warning=Instantiate(warningPrefab,point,Quaternion.identity);Destroy(warning,.8f);}
            }
            if(!z.activated||z.cleared||Time.time<z.readyAt)continue;
            while(z.spawned<z.enemies.Length&&alive<maximumAlive)
            {
                int n=z.spawned++;var go=Instantiate(z.enemies[n],z.spawnPoints[n],Quaternion.identity);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);
                var enemy=go.GetComponent<EnemyData>();if(enemy!=null){z.living.Add(enemy);alive++;}
            }
            if(z.spawned==z.enemies.Length&&z.living.Count==0)z.cleared=true;
        }
        if(!MainRouteCleared)return;
        foreach(var z in zones)if(z.activated&&!z.cleared)return;
        if(AliveCount>0)return;
        Completed=true;if(exitPortal!=null)exitPortal.SetActive(true);
        var rewards=StageRewardController.Ensure();if(rewards!=null)rewards.MarkCleared();
    }
}
