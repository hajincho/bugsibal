using UnityEngine;

public sealed class LunarArrowImpact : MonoBehaviour
{
    LunarBowSkill skill;
    CrystalTracer tracer;
    Arrow arrow;
    Rigidbody2D body;
    Vector3 launchTip;
    Vector2 direction;
    float nextSpark;
    bool impacted;
    int sparkIndex;
    public bool FullChargeSparkles { get; private set; }
    public static Color SparkColor(int index){return index%3==0 ? Color.white : index%3==1 ? new Color(1f,.84f,.57f) : new Color(.7f,.88f,1f);}
    public void Initialize(LunarBowSkill source,bool fullCharge=false)
    {
        FullChargeSparkles=fullCharge;
        skill=source;arrow=GetComponent<Arrow>();body=GetComponent<Rigidbody2D>();direction=body.velocity.normalized;
        Vector3 tip=CrystalTracer.LocalFront(transform);
        foreach(var s in GetComponentsInChildren<SpriteRenderer>(true))s.enabled=false;
        tracer=gameObject.AddComponent<CrystalTracer>();tracer.PhysicsTip=true;tracer.Initialize(transform,tip,source.trailMaterial,source.sparkleMaterial,false);
        tracer.Trail.time=.5f;tracer.Trail.startWidth=.1f;launchTip=tracer.Tip;
    }
    void LateUpdate()
    {
        if(impacted || tracer==null || skill==null || body==null || Time.time<nextSpark)return;
        nextSpark=Time.time+.035f;
        for(int i=0;i<3;i++)CrystalSparkle.Create(tracer.Tip,skill.sparkleMaterial,
            direction*Random.Range(5f,10f)+Vector2.Perpendicular(direction)*Random.Range(-3f,3f),
            FullChargeSparkles?Random.Range(1.078f,2.352f):Random.Range(.5f,1.1f),Random.Range(.45f,.85f),SparkColor(sparkIndex++));
    }
    bool bounced;
    Collider2D lastBounceWall;
    float bouncedAt;
    public bool TryBounce(Collider2D wall,Vector2 normal,Vector2 position,Vector2? surfacePoint)
    {
        if(skill==null || body==null || impacted)return false;
        if(bounced)return wall==lastBounceWall && Time.fixedTime<=bouncedAt+Time.fixedDeltaTime*1.5f;
        Vector2 incoming=body.velocity;
        if(incoming.sqrMagnitude<.001f)return false;
        if(normal.sqrMagnitude<.001f)normal=-incoming.normalized;
        normal.Normalize();if(Vector2.Dot(incoming,normal)>0)normal=-normal;
        Vector3 contact=surfacePoint.HasValue?(Vector3)surfacePoint.Value:(Vector3)position;
        // Use the incoming flight line for the corner, not the off-axis surface contact.
        Vector3 corner=launchTip+(Vector3)direction*Mathf.Max(0,Vector2.Dot(position-(Vector2)launchTip,direction));
        if(tracer!=null)tracer.ContinueFromBounce(corner);
        bounced=true;lastBounceWall=wall;bouncedAt=Time.fixedTime;
        Vector2 reflected=Vector2.Reflect(incoming,normal);
        if(Vector2.Dot(reflected.normalized,incoming.normalized)>.9999f)reflected=-incoming;
        body.position=position+normal*.06f;body.velocity=reflected;
        body.rotation=Mathf.Atan2(reflected.y,reflected.x)*Mathf.Rad2Deg+180f;
        direction=reflected.normalized;
        launchTip=corner;
        for(int i=0;i<30;i++)
        {
            float angle=(i+Random.value)*Mathf.PI*2/30;
            var drift=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*Random.Range(1.5f,4.5f);
            CrystalSparkle.Create(contact,skill.sparkleMaterial,drift,Random.Range(.8f,1.6f),Random.Range(.6f,1.1f),BurstColor(i));
        }
        return true;
    }

    public void OnImpact(EnemyData primary,Vector2 center,Vector2? surfacePoint)
    {
        if(impacted)return;impacted=true;
        Vector3 p=surfacePoint.HasValue ? (Vector3)surfacePoint.Value : (Vector3)center;
        Vector3 end=launchTip+(Vector3)direction*Mathf.Max(0,Vector2.Dot((Vector2)(p-launchTip),direction));
        if(tracer!=null)tracer.FinishAt(end);
        if(skill==null)return;
        if(primary!=null)
        {
            skill.OnEnemyHit(primary,p);
            if(primary.enemy_current_HP<=0f)skill.OnQualifiedEnemyDefeated(primary,primary.transform.position);
        }
        float radius=skill.GetComponent<BowExplosiveArrowSkill>()!=null ? skill.GetComponent<BowExplosiveArrowSkill>().explosionRadius : 3.25f;
        if(GetComponent<BowExplosiveArrowImpact>()==null)BurstSparkles(skill.sparkleMaterial,p,radius,FullChargeSparkles);
        // Contact keeps the radial sparkle effect; only the arrow's direct hit deals damage.
    }

    public void OnQualifiedEnemyDefeated(EnemyData enemy,Vector3 position){if(skill!=null)skill.OnQualifiedEnemyDefeated(enemy,position);}

    public void PlayQExplosion(Vector3 position,float radius)
    {
        if(skill!=null)LunarQMoonburst.Create(position,radius,skill.silverMaterial,skill.sparkleMaterial,FullChargeSparkles);
    }

    public static Color BurstColor(int index)
    {
        int slot=index%10;
        return slot<6?Color.white:slot<8?new Color(.83f,.85f,.89f):slot==8?new Color(1f,.94f,.72f):new Color(.76f,.9f,1f);
    }
    public static void BurstSparkles(Material material,Vector3 position,float radius,bool fullCharge=true)
    {
        int count=fullCharge?100:70;
        float sizeMultiplier=fullCharge?1f:.6f;
        int[] palette=new int[count];for(int i=0;i<palette.Length;i++)palette[i]=i%10;
        for(int i=palette.Length-1;i>0;i--){int j=Random.Range(0,i+1);int t=palette[i];palette[i]=palette[j];palette[j]=t;}
        float phase=Random.value*Mathf.PI*2;
        for(int i=0;i<count;i++)
        {
            float angle=phase+(i+Random.Range(-.35f,.35f))*Mathf.PI*2f/count;
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            var spark=CrystalSparkle.Create(position+(Vector3)(direction*Random.Range(0,.12f)),material,
                direction*Random.Range(radius*.8f,radius*2.2f),Random.Range(1.3f,2.9f)*sizeMultiplier,Random.Range(1.375f,2.875f),BurstColor(palette[i]));
            spark.name="LunarBurstSparkle";
        }
    }
}
