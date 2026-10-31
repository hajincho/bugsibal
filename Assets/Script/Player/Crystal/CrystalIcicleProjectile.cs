using UnityEngine;

public sealed class CrystalIcicleProjectile : MonoBehaviour
{
    EnemyData target;
    Vector2 position,direction,velocity,gravityCenter,aimDirection;
    float began,expires,radius,minimumDamage,healthFraction,skillMultiplier,hitRadius,initialGravity,flightSpeed;
    int color;
    bool straight,finished,hadTarget;

    Material material;
    SpriteRenderer sprite;
    CrystalTracer tracer;
    Sprite explosionSprite;
    readonly RaycastHit2D[] hits=new RaycastHit2D[64];
    public bool IsStraight => straight;
    public EnemyData AssignedTarget => target;
    public int ColorIndex => color;
    public void Initialize(CrystalBowSkill skill,BowIcicleSkill original,int tint,
        EnemyData assigned,Vector2 tangent,Vector2 outward,float rotationSign)
    {
        target=assigned;hadTarget=assigned!=null;color=tint;material=skill.glowMaterials[color];
        explosionSprite=skill.explosionSprites!=null && color<skill.explosionSprites.Length ? skill.explosionSprites[color] : null;
        position=transform.position;began=Time.time;expires=began+3f;
        gravityCenter=(Vector2)(skill.transform.position+original.centerOffset);
        Vector2 radial=position-gravityCenter;
        float distance=Mathf.Max(.3f,radial.magnitude);
        velocity=tangent;
        if(velocity.sqrMagnitude<.01f)
        {
            Vector2 radialDirection=radial.sqrMagnitude>.001f ? radial.normalized : outward.normalized;
            velocity=Vector2.Perpendicular(radialDirection)*(rotationSign==0f?1f:rotationSign)*Mathf.Max(5f,distance*6f);
        }
        direction=velocity.normalized;
        flightSpeed=velocity.magnitude;
        initialGravity=velocity.sqrMagnitude*distance;
        aimDirection=Vector2.zero;
        if(hadTarget)
        {
            Vector2 aim=CrystalEffects.TargetPoint(target)-position;
            aimDirection=((Vector2)(Quaternion.Euler(0,0,Random.Range(-3f,3f))*aim)).normalized;
        }
        radius=skill.ExplosionRadius*.25f;
        minimumDamage=original.damage;healthFraction=original.maxHealthDamageFraction;
        skillMultiplier=GameManager.BowSkillDamageMultiplier;hitRadius=Mathf.Min(.13f,original.hitRadius);
        sprite=GetComponent<SpriteRenderer>();sprite.enabled=true;sprite.sharedMaterial=skill.spriteMaterials[color];
        sprite.color=Color.white;
        var old=GetComponent<TrailRenderer>();if(old!=null){old.emitting=false;old.Clear();}
        tracer=gameObject.AddComponent<CrystalTracer>();
        Vector3 tip=new Vector3(0,-sprite.sprite.bounds.size.y*.5f,0);
        tracer.Initialize(transform,tip,skill.trailMaterials[color],skill.sparkleMaterial,false);
    }
    void FixedUpdate()
    {
        if(finished || StageRewardController.InputBlocked)return;
        if(Time.time>=expires){Finish(position);return;}
        if(hadTarget && (target==null || target.enemy_current_HP<=0f))straight=true;
        float age=Mathf.Max(0f,Time.time-began);
        if(age>=1.6f)straight=true;
        Vector2 next=position;
        if(straight)next+=velocity*Time.fixedDeltaTime;
        else CrystalOrbitDynamics.Step(ref next,ref velocity,gravityCenter,initialGravity,
            Mathf.Max(0f,age-Time.fixedDeltaTime),Time.fixedDeltaTime,aimDirection,hadTarget,flightSpeed);
        Vector2 travel=next-position;
        if(travel.sqrMagnitude>.00001f)direction=travel.normalized;
        int count=Physics2D.CircleCastNonAlloc(position,hitRadius,direction,hits,travel.magnitude);
        int chosen=-1;float nearest=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            var c=hits[i].collider;if(c==null || c.GetComponentInParent<Player>()!=null)continue;
            var enemy=c.GetComponentInParent<EnemyData>();
            bool validEnemy=enemy!=null && enemy.enemy_current_HP>0f && enemy.CompareTag("Enemy");
            bool wall=enemy==null && !c.isTrigger && (c.attachedRigidbody==null ||
                c.attachedRigidbody.bodyType==RigidbodyType2D.Static);
            if((validEnemy||wall)&&hits[i].distance<nearest){nearest=hits[i].distance;chosen=i;}
        }
        if(chosen>=0)
        {
            var hit=hits[chosen];var enemy=hit.collider.GetComponentInParent<EnemyData>();
            if(enemy!=null && enemy.enemy_current_HP>0f)
                enemy.TakeDamage(Mathf.Max(minimumDamage,enemy.enemy_HP*healthFraction)*skillMultiplier,CombatDamageKind.Skill);
            Vector2 impact=hit.distance>0f ? hit.point : position;
            CrystalEffects.Burst(color,impact,radius,material,enemy,.25f,true,explosionSprite);
            Finish(impact);return;
        }
        position=next;transform.position=next;
        transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg+90f);
        if(tracer!=null)tracer.SampleTip();
    }
    void Finish(Vector2 point)
    {
        if(finished)return;finished=true;
        if(sprite!=null)sprite.enabled=false;
        if(tracer!=null)tracer.FinishAt(point);
        Destroy(gameObject,.85f);
    }
}
