using UnityEngine;

public sealed class CrystalArrowImpact : MonoBehaviour
{
    public int ColorIndex { get; private set; }
    public bool HasExploded { get; private set; }
    public bool HasImpacted { get; private set; }
    public bool CanExplode { get; private set; }
    public Vector3 TraceEnd { get; private set; }
    float radius;
    Material material,sparkMaterial;
    CrystalTracer tracer;
    Sprite explosionSprite;
    Vector3 localTip,launchTip;
    Vector2 launchDirection;
    public void Initialize(CrystalBowSkill skill,int color,float effectRadius,bool fullCharge)
    {
        ColorIndex=color;radius=effectRadius;material=skill.glowMaterials[color];CanExplode=fullCharge;sparkMaterial=skill.sparkleMaterial;
        launchDirection=GetComponent<Rigidbody2D>().velocity.normalized;
        localTip=CrystalTracer.LocalFront(transform);
        foreach(var sprite in GetComponentsInChildren<SpriteRenderer>(true))sprite.enabled=false;
        tracer=gameObject.AddComponent<CrystalTracer>();
        tracer.Initialize(transform,localTip,skill.trailMaterials[color],skill.sparkleMaterial,fullCharge);
        launchTip=tracer.Tip;
        explosionSprite=skill.explosionSprites!=null && color<skill.explosionSprites.Length ? skill.explosionSprites[color] : null;
    }
    public void OnImpact(EnemyData primary,Vector2 contactCenter,Vector2? surfacePoint=null)
    {
        if(HasImpacted)return;HasImpacted=true;
        Vector3 position=surfacePoint.HasValue ? (Vector3)surfacePoint.Value :
            (Vector3)contactCenter+transform.TransformVector(localTip);
        // The collision surface can be off the centerline. Never bend the tracer toward that side contact.
        TraceEnd=launchTip+(Vector3)launchDirection*Mathf.Max(0f,Vector2.Dot((Vector2)(position-launchTip),launchDirection));
        if(tracer!=null)tracer.FinishAt(TraceEnd);
        if(!CanExplode)return;
        HasExploded=true;
        CrystalEffects.Burst(ColorIndex,position,radius,material,primary,1f,false,explosionSprite);
        CrystalEffects.BurstSparkles(position,radius,ColorIndex,sparkMaterial);
    }
}
