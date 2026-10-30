using UnityEngine;

// Each recipient gets its own 0.8-second window; the previous host is discharged on transfer.
public sealed class CrystalShock : MonoBehaviour
{
    public static CrystalShock Current { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCurrent(){Current=null;}
    EnemyData enemy;
    Material material;
    LineRenderer lightning;
    float beganAt,expiresAt,transferAt,nextShapeAt;
    public bool IsActive { get; private set; }
    public float RemainingSeconds => IsActive ? Mathf.Max(0,expiresAt-Time.time) : 0f;
    void Awake(){enemy=GetComponent<EnemyData>();}
    public void Apply(Material glow)
    {
        if(enemy==null || enemy.enemy_current_HP<=0f) return;
        if(Current!=null && Current!=this && Current.IsActive)Current.End(true);
        Current=this;
        material=glow;beganAt=Time.time;expiresAt=beganAt+.8f;
        transferAt=beganAt+Random.Range(.2f,.8f);
        IsActive=true;enabled=true;
        if(lightning==null)
        {
            var go=new GameObject("CrystalElectricAura");
            go.transform.SetParent(transform,false);
            lightning=go.AddComponent<LineRenderer>();
            lightning.positionCount=11;lightning.useWorldSpace=true;lightning.loop=false;
            lightning.startWidth=.045f;lightning.endWidth=.025f;
            lightning.sortingOrder=30;
        }
        lightning.sharedMaterial=material;lightning.enabled=true;
        lightning.startColor=Color.white;lightning.endColor=Color.white;
        nextShapeAt=0f;UpdateVisual();
    }
    void Update()
    {
        if(!IsActive)return;
        if(enemy==null || enemy.enemy_current_HP<=0f){End(false);return;}
        if(StageRewardController.InputBlocked)return;
        if(Time.time>=expiresAt){End(true);return;}
        UpdateVisual();
        if(Time.time>=transferAt && !TryTransfer())transferAt=Time.time+.06f;
    }
    public static Vector2 TransferAreaSize
    {
        get
        {
            var player=GameObject.FindGameObjectWithTag("Player");
            var loadout=player!=null ? player.GetComponent<WeaponSkillLoadout>() : null;
            var slot=loadout!=null && loadout.swordSkills.Length>0 ? loadout.swordSkills[0] : null;
            var skill=slot!=null ? slot.skill as Skill5 : null;
            var box=skill!=null && skill.swordeffect!=null ? skill.swordeffect.GetComponentInChildren<BoxCollider2D>(true) : null;
            if(box==null)return new Vector2(8.64f,5.76f);
            Vector3 scale=box.transform.lossyScale;
            return new Vector2(box.size.x*Mathf.Abs(scale.x),box.size.y*Mathf.Abs(scale.y));
        }
    }
    public bool TryTransfer()
    {
        if(!IsActive || Time.time<beganAt+.2f || Time.time>=expiresAt)return false;
        EnemyData chosen=null;float distance=float.PositiveInfinity;
        Vector2 center=CrystalEffects.TargetPoint(enemy);
        var seen=new System.Collections.Generic.HashSet<EnemyData>();
        foreach(var hit in Physics2D.OverlapBoxAll(center,TransferAreaSize,0f))
        {
            var candidate=hit.GetComponentInParent<EnemyData>();
            if(candidate==null || candidate==enemy || !candidate.CompareTag("Enemy") || candidate.enemy_current_HP<=0f || !seen.Add(candidate))continue;
            var status=candidate.GetComponent<CrystalShock>();
            if(status!=null && status.IsActive)continue;
            float d=(CrystalEffects.TargetPoint(candidate)-CrystalEffects.TargetPoint(enemy)).sqrMagnitude;
            if(d<=distance){distance=d;chosen=candidate;}
        }
        if(chosen==null)return false;
        var next=chosen.GetComponent<CrystalShock>();
        if(next==null)next=chosen.gameObject.AddComponent<CrystalShock>();
        Vector2 from=CrystalEffects.TargetPoint(enemy);
        Vector2 to=CrystalEffects.TargetPoint(chosen);
        End(true);
        next.Apply(material);
        CrystalLightningArc.Create(from,to,material);
        return true;
    }
    void UpdateVisual()
    {
        if(lightning==null || enemy==null)return;
        float pulse=.5f+.5f*Mathf.Sin((Time.time-beganAt)*Mathf.PI*20f);
        lightning.enabled=pulse>.2f;
        lightning.startColor=new Color(1,1,1,.3f+.7f*pulse);
        lightning.endColor=lightning.startColor;
        lightning.startWidth=.025f+.04f*pulse;
        lightning.endWidth=.015f+.025f*pulse;
        Vector2 center=CrystalEffects.TargetPoint(enemy);
        if(Time.time<nextShapeAt)return;
        nextShapeAt=Time.time+.045f;
        for(int i=0;i<11;i++)
        {
            float angle=i*Mathf.PI*.2f;
            Vector2 point=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*
                (i%2==0 ? .45f : .22f);
            lightning.SetPosition(i,point+Random.insideUnitCircle*.07f);
        }
    }
    void End(bool damage)
    {
        if(!IsActive)return;
        IsActive=false;if(Current==this)Current=null;
        if(lightning!=null)lightning.enabled=false;
        if(damage && enemy!=null && enemy.enemy_current_HP>0f)enemy.TakeDamage(15f,CombatDamageKind.Skill);
    }
    void OnDisable(){End(false);}
}

public sealed class CrystalLightningArc : MonoBehaviour
{
    float expires;
    public static void Create(Vector2 start,Vector2 end,Material material)
    {
        var root=new GameObject("CrystalLightningTransfer");
        var line=root.AddComponent<LineRenderer>();
        line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=9;
        line.startWidth=.055f;line.endWidth=.025f;line.sortingOrder=31;
        line.startColor=line.endColor=Color.white;
        Vector2 side=Vector2.Perpendicular(end-start).normalized;
        for(int i=0;i<9;i++)line.SetPosition(i,Vector2.Lerp(start,end,i/8f)+side*
            (i==0||i==8 ? 0f : Random.Range(-.16f,.16f)));
        root.AddComponent<CrystalLightningArc>().expires=Time.time+.16f;
    }
    void Update(){if(Time.time>=expires)Destroy(gameObject);}
}
