using System.Collections.Generic;
using UnityEngine;

public sealed class LunarClone : MonoBehaviour
{
    static readonly List<LunarClone> all=new List<LunarClone>();
    public LunarBowSkill Source { get; private set; }
    public float Health { get; private set; }
    public bool Archer { get; private set; }
    public float MaxHealth => Archer?10f:20f;
    public Vector2 AimPoint => (Vector2)transform.position+Vector2.up*.75f;
    LunarEchoRig rig;
    float born,decayedSeconds,resolveAttackAt;
    bool pendingAttack;
    EnemyData attackTarget;
    Vector2 attackPoint;
    public Vector2 ProjectileOrigin => rig!=null?(Vector2)rig.Muzzle:AimPoint;
    Rigidbody2D body;
    CapsuleCollider2D hitbox;
    EnemyData target;
    float nextScan,nextAttack,attackStart=-99,followAngle;
    Vector2 motion;
    readonly List<Transform> visuals=new List<Transform>();
    readonly List<Vector3> rest=new List<Vector3>();
    readonly Dictionary<EnemyData,float> hitTimes=new Dictionary<EnemyData,float>();
    Transform weapon;
    readonly RaycastHit2D[] walls=new RaycastHit2D[24];
    public static LunarClone Create(LunarBowSkill skill,Vector3 position,bool archer)
    {
        var go=new GameObject(archer?"LunarArcherEcho":"LunarSwordEcho");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,skill.gameObject.scene);
        go.transform.position=position;
        var sorting=go.AddComponent<UnityEngine.Rendering.SortingGroup>();
        var sourceSorting=skill.Owner.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
        if(sourceSorting!=null){sorting.sortingLayerID=sourceSorting.sortingLayerID;sorting.sortingOrder=sourceSorting.sortingOrder;}
        var c=go.AddComponent<LunarClone>();c.Source=skill;c.Archer=archer;c.Health=archer?10:20;c.born=Time.time;
        c.body=go.AddComponent<Rigidbody2D>();c.body.gravityScale=0;c.body.freezeRotation=true;
        c.body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        c.hitbox=go.AddComponent<CapsuleCollider2D>();c.hitbox.size=new Vector2(.7f,1.6f);c.hitbox.offset=new Vector2(0,.8f);
        c.followAngle=Random.Range(0,Mathf.PI*2);c.BuildVisuals();
        foreach(var other in all)if(other!=null)Physics2D.IgnoreCollision(c.hitbox,other.hitbox);
        foreach(var col in skill.Owner.GetComponentsInChildren<Collider2D>())Physics2D.IgnoreCollision(c.hitbox,col);
        all.Add(c);if(CombatPresentation.Instance!=null)CombatPresentation.Instance.RegisterClone(c);return c;
    }
    public static void Clear(LunarBowSkill owner)
    {
        foreach(var c in all.ToArray())if(c!=null && c.Source==owner){c.Health=0;Destroy(c.gameObject);}
    }
    public static void HurtInCone(Vector2 origin,Vector2 direction,float length,float width,float damage)
    {
        foreach(var c in all.ToArray())
        {
            if(c==null || c.Health<=0)continue;
            Vector2 delta=c.AimPoint-origin;float along=Vector2.Dot(delta,direction);
            if(along<0 || along>length)continue;
            float half=Mathf.Lerp(.3f,width*.5f,along/Mathf.Max(.01f,length));
            if(Mathf.Abs(delta.x*direction.y-delta.y*direction.x)<=half)c.Hurt(damage);
        }
    }
    public static Transform ResolveTarget(Vector3 point,Transform player)
    {
        if(player==null)return null;
        Transform best=player;float distance=((Vector2)(player.position-point)).sqrMagnitude;
        foreach(var c in all)
        {
            if(c==null || c.Health<=0 || !c.isActiveAndEnabled || c.Source==null || !c.Source.IsEquipped)continue;
            float d=(c.AimPoint-(Vector2)point).sqrMagnitude;
            if(d>=distance || !ClearLine(point,c.AimPoint))continue;
            best=c.transform;distance=d;
        }
        return best;
    }
    public static Vector3 TargetPosition(Component enemy,Transform player)
    {var t=ResolveTarget(enemy.transform.position,player);return t!=null?t.position:enemy.transform.position;}
    static bool ClearLine(Vector2 from,Vector2 to)
    {
        foreach(var h in Physics2D.LinecastAll(from,to))
            if(h.collider!=null && !h.collider.isTrigger && h.collider.GetComponentInParent<EnemyData>()==null &&
                h.collider.GetComponentInParent<LunarClone>()==null && h.collider.GetComponentInParent<Player>()==null &&
                (h.rigidbody==null || h.rigidbody.bodyType==RigidbodyType2D.Static))return false;
        return true;
    }
    void BuildVisuals(){rig=gameObject.AddComponent<LunarEchoRig>();rig.Initialize(this);}
    void Update()
    {
        if(Source==null || !Source.IsEquipped || GameManager.player_current_HP<=0){Health=0;Destroy(gameObject);return;}
        if(StageRewardController.InputBlocked || Time.timeScale<=0){motion=Vector2.zero;body.velocity=Vector2.zero;return;}
        float elapsed=Mathf.Max(0,Time.time-born-3f);
        float decay=Mathf.Max(0,elapsed-decayedSeconds);decayedSeconds=elapsed;
        Health=Mathf.Max(0,Health-decay);if(Health<=0){Die();return;}
        if(pendingAttack && Time.time>=resolveAttackAt)
        {
            pendingAttack=false;
            if(Archer)LunarEchoBolt.Create(this,attackTarget!=null && attackTarget.enemy_current_HP>0?CrystalEffects.TargetPoint(attackTarget):attackPoint);
            else if(attackTarget!=null && attackTarget.enemy_current_HP>0 && Vector2.Distance(AimPoint,CrystalEffects.TargetPoint(attackTarget))<=1.9f && CanSee(CrystalEffects.TargetPoint(attackTarget)))
                DamageEnemy(attackTarget,10);
        }
        if(Time.time>=nextScan)
        {
            nextScan=Time.time+.25f;target=null;float best=12*12;
            foreach(var e in CrystalEffects.Targets(AimPoint,12))
            {
                if(e.GetComponent<TrainingDummy>()!=null)continue;
                float d=(CrystalEffects.TargetPoint(e)-AimPoint).sqrMagnitude;
                if(d<best){best=d;target=e;}
            }
        }
        Vector2 destination;
        if(target!=null && target.enemy_current_HP>0)
        {
            destination=CrystalEffects.TargetPoint(target)-Vector2.up*.7f;
            float range=Archer?6f:1.5f;
            float distance=Vector2.Distance(AimPoint,CrystalEffects.TargetPoint(target));
            motion=distance>range ? (destination-(Vector2)transform.position).normalized*3.2f : Vector2.zero;
            if(distance<=range+.15f && Time.time>=nextAttack && CanSee(CrystalEffects.TargetPoint(target)))
            {
                nextAttack=Time.time+(Archer?2f:1.5f);attackStart=Time.time;
                pendingAttack=true;attackTarget=target;attackPoint=CrystalEffects.TargetPoint(target);resolveAttackAt=Time.time+(Archer?.3f:.18f);rig.BeginAttack();
            }
        }
        else
        {
            destination=(Vector2)Source.transform.position+new Vector2(Mathf.Cos(followAngle),Mathf.Sin(followAngle))*2.8f;
            motion=Vector2.Distance(transform.position,destination)>.6f ? (destination-(Vector2)transform.position).normalized*3.2f : Vector2.zero;
        }
        float dx=target!=null?CrystalEffects.TargetPoint(target).x-AimPoint.x:motion.x;
        if(Mathf.Abs(dx)>.05f)transform.localScale=new Vector3(dx>0?-1:1,1,1);
        if(rig.IsAttacking)motion=Vector2.zero;
        Pose();
    }
    bool CanSee(Vector2 point)
    {
        return ClearLine(AimPoint,point);
    }
    void FixedUpdate()
    {
        if(body==null || Health<=0)return;
        if(StageRewardController.InputBlocked){body.velocity=Vector2.zero;return;}
        Vector2 velocity=motion;
        if(velocity.sqrMagnitude>0)
        {
            var filter=new ContactFilter2D();filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));filter.useTriggers=false;
            int count=body.Cast(velocity.normalized,filter,walls,velocity.magnitude*Time.fixedDeltaTime+.05f);
            for(int i=0;i<count;i++)
                if(walls[i].collider.GetComponentInParent<EnemyData>()==null && walls[i].collider.GetComponentInParent<LunarClone>()==null)
                    velocity-=walls[i].normal*Mathf.Min(0,Vector2.Dot(velocity,walls[i].normal));
        }
        body.velocity=velocity;
    }
    void Pose(){if(rig!=null)rig.SetMoving(motion.sqrMagnitude>.01f);}
    public static void DamageEnemy(EnemyData enemy,float amount)
    {
        if(enemy==null)return;
        bool previous=LunarBowSkill.SuppressKillRewards;LunarBowSkill.SuppressKillRewards=true;
        try{enemy.TakeDamage(amount,CombatDamageKind.Normal);enemy.Hitted();}
        finally{LunarBowSkill.SuppressKillRewards=previous;}
    }
    public void Hurt(float amount)
    {
        if(amount<=0 || Health<=0 || StageRewardController.InputBlocked)return;
        Health=Mathf.Max(0,Health-amount);
        CombatPresentation.ShowDamage(transform.position+Vector3.up*2f,amount,CombatDamageKind.Normal);
        if(Health<=0)Die();
    }
    void Die(){hitbox.enabled=false;body.simulated=false;Destroy(gameObject);}
    void Contact(Collider2D collider)
    {
        var e=collider.GetComponentInParent<EnemyData>();
        if(e==null || e.enemy_current_HP<=0 || EnemyFrostStatus.IsFrozenOn(e) || e.GetComponent<TrainingDummy>()!=null || e.GetComponent<SwordMan>()!=null || e.GetComponent<Dragon>()!=null)return;
        float next;if(hitTimes.TryGetValue(e,out next) && Time.time<next)return;
        hitTimes[e]=Time.time+1f;Hurt(e.enemy_power);
    }
    void OnCollisionStay2D(Collision2D collision){Contact(collision.collider);}
    void OnTriggerStay2D(Collider2D collider){Contact(collider);}
    void OnDestroy(){if(CombatPresentation.Instance!=null)CombatPresentation.Instance.UnregisterClone(this);all.Remove(this);foreach(var t in visuals)if(t!=null){var s=t.GetComponent<SpriteRenderer>().sprite;if(s!=null)Destroy(s);}}
}

public sealed class LunarEchoBolt : MonoBehaviour
{
    Vector2 velocity;
    float expires;
    TrailRenderer trail;
    public static void Create(LunarClone owner,Vector2 target)
    {
        var go=new GameObject("LunarEchoArrow",typeof(TrailRenderer));go.transform.position=owner.ProjectileOrigin;
        var b=go.AddComponent<LunarEchoBolt>();b.velocity=(target-owner.ProjectileOrigin).normalized*25;b.expires=Time.time+2f;
        b.trail=go.GetComponent<TrailRenderer>();b.trail.sharedMaterial=owner.Source.trailMaterial;
        b.trail.time=.2f;b.trail.startWidth=.055f;b.trail.endWidth=0;b.trail.sortingOrder=20;
        b.trail.startColor=new Color(.7f,.75f,.8f);b.trail.endColor=new Color(.7f,.75f,.8f,0);
    }
    void FixedUpdate()
    {
        if(StageRewardController.InputBlocked)return;
        if(Time.time>=expires){Finish();return;}
        Vector2 step=velocity*Time.fixedDeltaTime;
        foreach(var h in Physics2D.RaycastAll(transform.position,step.normalized,step.magnitude))
        {
            if(h.collider.GetComponentInParent<LunarClone>()!=null || h.collider.GetComponentInParent<Player>()!=null ||
                h.collider.GetComponentInParent<Arrow>()!=null)continue;
            var e=h.collider.GetComponentInParent<EnemyData>();
            if(e!=null && e.enemy_current_HP>0){LunarClone.DamageEnemy(e,10);transform.position=h.point;Finish();return;}
            if(e==null && !h.collider.isTrigger && (h.rigidbody==null || h.rigidbody.bodyType==RigidbodyType2D.Static)){transform.position=h.point;Finish();return;}
        }
        transform.position+=(Vector3)step;
    }
    void Finish(){velocity=Vector2.zero;trail.emitting=false;enabled=false;Destroy(gameObject,.25f);}
}
