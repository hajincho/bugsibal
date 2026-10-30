using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(25000)]
public sealed class LunarBowSkill : MonoBehaviour
{
    public BowChargeSettings profile;
    public JusticeCharacterSkin skin;
    public Sprite[] modeIcons;
    public Sprite burstSprite, orbSprite, haloSprite, swordSprite, echoBowSprite;
    public Material echoWeaponMaterial;
    public Material trailMaterial, sparkleMaterial, silverMaterial;
    public GameObject hud;
    public UnityEngine.UI.Image icon, cooldownMask;
    public TMPro.TextMeshProUGUI modeLabel;
    public float cooldownSeconds=.2f;
    public int Mode { get; private set; } = 1;
    public float RemainingCooldown => Mathf.Max(0,readyAt-Time.time);
    public float MoveMultiplier => IsEquipped && Time.time<hasteUntil ? 1.1f : 1f;
    public bool IsEquipped => owner!=null && owner.EquippedBowItem!=null && owner.EquippedBowItem.bowSettings==profile;
    public bool IsActiveBow => IsEquipped && owner.weapon_mode==0;
    public Player Owner => owner;
    public static bool SuppressKillRewards;
    Player owner;
    float readyAt,hasteUntil;
    SpriteRenderer halo;
    Vector3 haloPosition,haloVelocity;
    bool haloFollowing;
    bool hadEquipped;
    static readonly string[] Stages={"I","II","III"};
    void Awake(){owner=GetComponent<Player>();}
    void OnEnable(){EnemyData.Died+=OnEnemyDied;SceneManager.activeSceneChanged+=SceneChanged;}
    void OnDisable(){EnemyData.Died-=OnEnemyDied;SceneManager.activeSceneChanged-=SceneChanged;Cleanup();if(halo!=null)halo.enabled=false;if(hud!=null)hud.SetActive(false);}
    void OnDestroy(){if(halo!=null)Destroy(halo.gameObject);}
    void SceneChanged(Scene a,Scene b){Cleanup();}
    void Cleanup(){LunarClone.Clear(this);foreach(var orb in FindObjectsOfType<LunarSoulOrb>())if(orb.Source==this)Destroy(orb.gameObject);hasteUntil=0;}
    public void ResetForEquip(Item item){Mode=1;readyAt=0;hasteUntil=0;}
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))TryCycle();
        if(hadEquipped && !IsEquipped)Cleanup();
        hadEquipped=IsEquipped;
        if(GameManager.player_current_HP<=0)Cleanup();
    }
    public bool TryCycle()
    {
        if(!IsActiveBow || RemainingCooldown>0 || StageRewardController.InputBlocked || Time.timeScale<=0 ||
            GameManager.player_current_HP<=0 || owner.on_dash || owner.skill_using)return false;
        owner.chargeBow.CancelCharge();Mode=Mode%3+1;readyAt=Time.time+cooldownSeconds;return true;
    }
    void LateUpdate()
    {
        if(hud!=null)hud.SetActive(IsActiveBow);
        if(icon!=null && modeIcons!=null && modeIcons.Length>=Mode)icon.sprite=modeIcons[Mode-1];
        if(modeLabel!=null)modeLabel.text=Stages[Mode-1];
        if(cooldownMask!=null)cooldownMask.fillAmount=cooldownSeconds>0 ? RemainingCooldown/cooldownSeconds : 0;
        if(halo==null && haloSprite!=null)
        {
            var go=new GameObject("ArtemisiaMoonHalo",typeof(SpriteRenderer));
            SceneManager.MoveGameObjectToScene(go,gameObject.scene);
            halo=go.GetComponent<SpriteRenderer>();halo.sprite=haloSprite;halo.sortingOrder=-40;
        }
        if(halo!=null)
        {
            halo.enabled=IsEquipped;
            var sorting=GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
            if(sorting!=null){halo.sortingLayerID=sorting.sortingLayerID;halo.sortingOrder=sorting.sortingOrder-1;}
            if(halo.sprite!=haloSprite)halo.sprite=haloSprite;
            Vector3 target=skin!=null?skin.VisualHeadCenter+new Vector3(-.25f,.15f,0):transform.position+new Vector3(-.25f,1.65f,0);
            target+=Vector3.up*(.06f*Mathf.Sin(Time.time*1.5f));
            if(!IsEquipped)haloFollowing=false;
            if(!haloFollowing || (target-haloPosition).sqrMagnitude>16f){haloPosition=target;haloVelocity=Vector3.zero;haloFollowing=IsEquipped;}
            float remaining=Time.deltaTime;
            for(int i=0;i<8 && remaining>0;i++){float dt=Mathf.Min(.02f,remaining);haloVelocity+=(target-haloPosition)*(90f*dt);haloVelocity*=Mathf.Exp(-14f*dt);haloPosition+=haloVelocity*dt;remaining-=dt;}
            haloPosition=target+Vector3.ClampMagnitude(haloPosition-target,.4f);
            halo.transform.position=haloPosition;
            halo.transform.rotation=Quaternion.identity;
            float size=3.1f/Mathf.Max(.01f,halo.sprite.bounds.size.y);
            halo.transform.localScale=Vector3.one*size;
            halo.color=new Color(1f,1f,1f,.58f+.08f*Mathf.Sin(Time.time*2f));
        }
    }
    void OnEnemyDied(EnemyData enemy)
    {
        if(!IsEquipped || SuppressKillRewards || enemy==null || enemy.GetComponent<TrainingDummy>()!=null || GameManager.player_current_HP<=0)return;
        if(Mode<3)LunarClone.Create(this,enemy.transform.position,Mode==1);
        else if(Random.value<.5f)SpawnOrb(enemy.transform.position);
    }
    public LunarSoulOrb SpawnOrb(Vector3 position)
    {
        var go=new GameObject("LunarSoulOrb",typeof(SpriteRenderer));go.transform.position=position+Vector3.up*.4f;
        var orb=go.AddComponent<LunarSoulOrb>();orb.Initialize(this);return orb;
    }
    public void AbsorbSoul()
    {
        if(!IsEquipped || GameManager.player_current_HP<=0f)return;
        if(GameManager.player_current_HP>=GameManager.player_HP)
            RunUpgradeState.GrantShield(RunUpgradeState.Shield+5f);
        else GameManager.HealPlayer(10);
        hasteUntil=Time.time+2f;
    }
    public void OnArrowFired(Arrow arrow,float chargeSeconds=0f)
    {
        if(IsEquipped && arrow!=null)arrow.gameObject.AddComponent<LunarArrowImpact>().Initialize(this,profile.ChargeFraction(chargeSeconds)>=1f);
    }
}

public sealed class LunarSoulOrb : MonoBehaviour
{
    public LunarBowSkill Source { get; private set; }
    SpriteRenderer visual;
    const float Diameter=1.74f;
    float born,phase,nextSpark;
    int sparkIndex;
    public void Initialize(LunarBowSkill skill)
    {
        Source=skill;visual=GetComponent<SpriteRenderer>();visual.sprite=skill.orbSprite;visual.sortingOrder=28;
        born=Time.time;phase=Random.value*6.28f;
        transform.localScale=Vector3.one*(Diameter/Mathf.Max(.01f,visual.sprite.bounds.size.y));
        visual.color=new Color(1,1,1,.65f);
    }
    void Update()
    {
        if(Source==null || !Source.IsEquipped || GameManager.player_current_HP<=0){Destroy(gameObject);return;}
        if(StageRewardController.InputBlocked || Time.timeScale<=0)return;
        Vector3 target=Source.transform.position+Vector3.up*.7f;
        Vector3 previous=transform.position;
        transform.position=Vector3.MoveTowards(transform.position,target,2.2f*Time.deltaTime);
        if((transform.position-previous).sqrMagnitude>.000001f && Source.sparkleMaterial!=null && Time.time>=nextSpark)
        {
            nextSpark=Time.time+.06f;
            for(int i=0;i<2;i++)
            {
                var spark=CrystalSparkle.Create(Vector3.Lerp(previous,transform.position,(i+.5f)/2f)+(Vector3)(Random.insideUnitCircle*.05f),
                    Source.sparkleMaterial,Random.insideUnitCircle*.2f,Random.Range(.5f,.95f),Random.Range(.75f,1.25f),LunarArrowImpact.BurstColor(sparkIndex++));
                spark.name="LunarSoulSparkle";
            }
        }
        float pulse=1+.09f*Mathf.Sin((Time.time-born)*7+phase);
        transform.localScale=Vector3.one*(Diameter/Mathf.Max(.01f,visual.sprite.bounds.size.y)*pulse);
        visual.color=new Color(1,1,1,.5f+.2f*Mathf.Sin(Time.time*9+phase));
        if(Vector2.Distance(transform.position,target)<.3f){Source.AbsorbSoul();Destroy(gameObject);}
    }
}
