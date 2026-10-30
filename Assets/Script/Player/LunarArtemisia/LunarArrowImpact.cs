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
        tracer=gameObject.AddComponent<CrystalTracer>();tracer.Initialize(transform,tip,source.trailMaterial,source.sparkleMaterial,false);
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
    public void OnImpact(EnemyData primary,Vector2 center,Vector2? surfacePoint)
    {
        if(impacted)return;impacted=true;
        Vector3 p=surfacePoint.HasValue ? (Vector3)surfacePoint.Value : (Vector3)center;
        Vector3 end=launchTip+(Vector3)direction*Mathf.Max(0,Vector2.Dot((Vector2)(p-launchTip),direction));
        if(tracer!=null)tracer.FinishAt(end);
        if(skill==null)return;
        float radius=skill.GetComponent<BowExplosiveArrowSkill>()!=null ? skill.GetComponent<BowExplosiveArrowSkill>().explosionRadius : 3.25f;
        BurstSparkles(skill.sparkleMaterial,p,radius);
        // Contact keeps the radial sparkle effect; only the arrow's direct hit deals damage.
    }

    public static Color BurstColor(int index)
    {
        int slot=index%10;
        return slot<6?Color.white:slot<8?new Color(.83f,.85f,.89f):slot==8?new Color(1f,.94f,.72f):new Color(.76f,.9f,1f);
    }
    public static void BurstSparkles(Material material,Vector3 position,float radius)
    {
        int[] palette=new int[100];for(int i=0;i<palette.Length;i++)palette[i]=i%10;
        for(int i=palette.Length-1;i>0;i--){int j=Random.Range(0,i+1);int t=palette[i];palette[i]=palette[j];palette[j]=t;}
        float phase=Random.value*Mathf.PI*2;
        for(int i=0;i<100;i++)
        {
            float angle=phase+(i+Random.Range(-.35f,.35f))*Mathf.PI*2f/100f;
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            var spark=CrystalSparkle.Create(position+(Vector3)(direction*Random.Range(0,.12f)),material,
                direction*Random.Range(radius*.8f,radius*2.2f),Random.Range(1.3f,2.9f),Random.Range(.55f,1.15f),BurstColor(palette[i]));
            spark.name="LunarBurstSparkle";
        }
    }
}
