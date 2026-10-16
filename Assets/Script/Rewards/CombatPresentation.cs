using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum CombatDamageKind { Normal, Skill, Heal, PlayerHit }

public class CombatPresentation : MonoBehaviour
{
    public CombatFeatureConfig config;
    public static CombatPresentation Instance { get; private set; }
    class EnemyBar
    {
        public RectTransform root;
        public UnityEngine.UI.Image fill;
        public TextMeshProUGUI text;
        public float lastHP=-1,lastMax=-1;
    }
    readonly Dictionary<EnemyData,EnemyBar> bars=new Dictionary<EnemyData,EnemyBar>();
    readonly Stack<EnemyBar> spareBars=new Stack<EnemyBar>();
    readonly Stack<DamageText> spareNumbers=new Stack<DamageText>();
    readonly List<DamageText> activeNumbers=new List<DamageText>();
    readonly List<EnemyData> remove=new List<EnemyData>();
    RectTransform canvas,bossRoot;
    UnityEngine.UI.Image bossFill;
    TextMeshProUGUI bossName,bossHP;
    EnemyData boss;
    float lastBossHP=-1,lastBossMax=-1;
    const int MaxNumbers=96;

    void Awake()
    {
        if(Instance!=null && Instance!=this){enabled=false;return;}Instance=this;
        canvas=(RectTransform)FeatureUI.Canvas("CombatInfoCanvas",transform,60).transform;
        bossRoot=FeatureUI.Rect("BossHealthHUD",canvas,new Vector2(940,105),Vector2.zero);
        bossRoot.anchorMin=bossRoot.anchorMax=new Vector2(.5f,1);bossRoot.pivot=new Vector2(.5f,1);bossRoot.anchoredPosition=new Vector2(0,-20);
        FeatureUI.Image("BossBackground",bossRoot,new Vector2(940,100),Vector2.zero,new Color(.025f,.035f,.06f,.96f));
        bossName=FeatureUI.Text("BossName",bossRoot,new Vector2(900,36),new Vector2(0,27),"",29,config.font,new Color(1,.84f,.5f));
        var bg=FeatureUI.Image("BossTrack",bossRoot,new Vector2(880,30),new Vector2(0,-12),new Color(.15f,.06f,.09f));
        bossFill=FeatureUI.Image("BossFill",bg.transform,new Vector2(872,22),Vector2.zero,new Color(.88f,.19f,.24f));ConfigureFill(bossFill);
        bossHP=FeatureUI.Text("BossHP",bg.transform,new Vector2(860,30),Vector2.zero,"",23,config.font,Color.white);
        bossRoot.gameObject.SetActive(false);
    }
    void OnDestroy(){if(Instance==this)Instance=null;}
    void ConfigureFill(UnityEngine.UI.Image image)
    {
        image.sprite=config.healthFill;image.type=UnityEngine.UI.Image.Type.Filled;
        image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.fillOrigin=0;
    }
    public void Register(EnemyData enemy)
    {
        if(enemy==null || enemy.CompareTag("drumtong"))return;
        if(enemy.IsBoss){boss=enemy;lastBossHP=lastBossMax=-1;return;}
        if(bars.ContainsKey(enemy))return;
        EnemyBar bar;
        if(spareBars.Count>0)bar=spareBars.Pop();
        else
        {
            bar=new EnemyBar();bar.root=FeatureUI.Rect("EnemyHealth",canvas,new Vector2(148,40),Vector2.zero);
            var bg=FeatureUI.Image("Track",bar.root,new Vector2(136,13),new Vector2(0,7),new Color(.03f,.035f,.05f,.95f));
            bar.fill=FeatureUI.Image("Fill",bg.transform,new Vector2(130,7),Vector2.zero,new Color(.6f,.88f,.4f));ConfigureFill(bar.fill);
            bar.text=FeatureUI.Text("HP",bar.root,new Vector2(148,25),new Vector2(0,-12),"",19,config.font,Color.white);
        }
        bar.lastHP=bar.lastMax=-1;bar.root.gameObject.SetActive(true);bars.Add(enemy,bar);
    }
    public void Unregister(EnemyData enemy)
    {
        if(boss==enemy){boss=null;bossRoot.gameObject.SetActive(false);}
        EnemyBar bar;
        if(enemy!=null && bars.TryGetValue(enemy,out bar)){bar.root.gameObject.SetActive(false);spareBars.Push(bar);bars.Remove(enemy);}
    }
    public void SceneChanged()
    {
        foreach(var pair in bars){pair.Value.root.gameObject.SetActive(false);spareBars.Push(pair.Value);}
        bars.Clear();boss=null;if(bossRoot!=null)bossRoot.gameObject.SetActive(false);
        for(int i=activeNumbers.Count-1;i>=0;i--)if(activeNumbers[i]!=null)activeNumbers[i].Recycle();
    }
    public static Vector3 HeadPosition(Transform target)
    {
        var renderer=target.GetComponentInChildren<SpriteRenderer>();
        return renderer!=null?new Vector3(target.position.x,renderer.bounds.max.y+.4f,target.position.z):target.position+Vector3.up*2;
    }
    public bool Project(Vector3 world,out Vector2 point)
    {
        point=Vector2.zero;var camera=Camera.main;if(camera==null)return false;
        var screen=camera.WorldToScreenPoint(world);if(screen.z<0)return false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,screen,null,out point);
        return screen.x>=0 && screen.x<=Screen.width && screen.y>=0 && screen.y<=Screen.height;
    }
    public static void ShowDamage(Vector3 position,float amount,CombatDamageKind kind)
    {if(Instance!=null && amount>0)Instance.SpawnNumber(position,amount,kind);}
    void SpawnNumber(Vector3 position,float amount,CombatDamageKind kind)
    {
        DamageText number;
        if(spareNumbers.Count>0)number=spareNumbers.Pop();
        else if(activeNumbers.Count<MaxNumbers)
        {
            var text=FeatureUI.Text("DamageNumber",canvas,new Vector2(220,60),Vector2.zero,"",30,config.font,Color.white);
            text.fontStyle=FontStyles.Bold;text.outlineWidth=.16f;text.outlineColor=new Color32(10,12,19,255);
            number=text.gameObject.AddComponent<DamageText>();
        }
        else{number=activeNumbers[0];number.Recycle();spareNumbers.Pop();}
        activeNumbers.Add(number);number.gameObject.SetActive(true);
        number.SetupPooled(amount,kind,position+new Vector3(UnityEngine.Random.Range(-.25f,.25f),0,0),this);
    }
    public void ReturnNumber(DamageText number)
    {activeNumbers.Remove(number);number.gameObject.SetActive(false);spareNumbers.Push(number);}
    void LateUpdate()
    {
        remove.Clear();
        foreach(var pair in bars)
        {
            var enemy=pair.Key;var bar=pair.Value;
            if(enemy==null){remove.Add(enemy);bar.root.gameObject.SetActive(false);spareBars.Push(bar);continue;}
            Vector2 point=Vector2.zero;
            bool visible=enemy.gameObject.activeInHierarchy && enemy.enemy_current_HP>0 && Project(HeadPosition(enemy.transform),out point);
            bar.root.gameObject.SetActive(visible);if(!visible)continue;bar.root.anchoredPosition=point;
            if(!Mathf.Approximately(bar.lastHP,enemy.enemy_current_HP)||!Mathf.Approximately(bar.lastMax,enemy.enemy_HP))
            {bar.fill.fillAmount=enemy.enemy_HP>0?Mathf.Clamp01(enemy.enemy_current_HP/enemy.enemy_HP):0;bar.text.text=string.Format("{0:0.#} / {1:0.#}",Mathf.Max(0,enemy.enemy_current_HP),enemy.enemy_HP);bar.lastHP=enemy.enemy_current_HP;bar.lastMax=enemy.enemy_HP;}
        }
        foreach(var enemy in remove)bars.Remove(enemy);
        bool showBoss=boss!=null && boss.gameObject.activeInHierarchy && boss.enemy_current_HP>0;bossRoot.gameObject.SetActive(showBoss);if(!showBoss)return;
        if(!Mathf.Approximately(lastBossHP,boss.enemy_current_HP)||!Mathf.Approximately(lastBossMax,boss.enemy_HP))
        {
            bossName.text=SceneManager.GetActiveScene().name=="MidBoss"?"중간 보스":"최종 보스";
            bossFill.fillAmount=boss.enemy_HP>0?Mathf.Clamp01(boss.enemy_current_HP/boss.enemy_HP):0;
            bossHP.text=string.Format("{0:0.#} / {1:0.#}",Mathf.Max(0,boss.enemy_current_HP),boss.enemy_HP);
            lastBossHP=boss.enemy_current_HP;lastBossMax=boss.enemy_HP;
        }
    }
}
