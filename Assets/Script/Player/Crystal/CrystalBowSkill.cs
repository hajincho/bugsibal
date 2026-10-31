using UnityEngine;
using System.Collections.Generic;

public sealed class CrystalBowSkill : MonoBehaviour
{
    public BowChargeSettings profile;
    public Material[] spriteMaterials = new Material[3];
    public Material[] glowMaterials = new Material[3];
    public Material[] trailMaterials = new Material[3];
    public Material sparkleMaterial;
    public Sprite[] explosionSprites = new Sprite[3];
    public GameObject hud;
    public UnityEngine.UI.Image icon;
    public UnityEngine.UI.Image cooldownMask;
    public TMPro.TMP_Text cooldownText;
    public float targetRadius = 8f;
    public float cooldownSeconds = 32f;
    float readyAt;
    public float RemainingCooldown => Mathf.Max(0f,readyAt-Time.time);
    public int NextArrowColor { get; private set; }
    Player owner;
    public bool IsEquipped => owner != null && owner.EquippedBowItem != null &&
        owner.EquippedBowItem.bowSettings == profile;
    public bool IsActiveBow => IsEquipped && owner.weapon_mode == 0;
    public bool CanReleaseOrbit => IsActiveBow && RemainingCooldown<=0f &&
        owner.GetComponent<BowIcicleSkill>() != null &&
        owner.GetComponent<BowIcicleSkill>().ActiveOrbit != null &&
        owner.GetComponent<BowIcicleSkill>().ActiveOrbit.LiveCount > 0;
    public static Color ColorAt(int index)
    {
        return index == 0 ? new Color(.32f,1f,.78f) :
            index == 1 ? new Color(.28f,.8f,1f) : new Color(.94f,.65f,1f);
    }
    void Awake() { owner = GetComponent<Player>(); }
    void Update() { if(Input.GetKeyDown(KeyCode.C)) TryCast(); }
    void LateUpdate() { RefreshHUD(); }
    public void ResetForEquip(Item item) { NextArrowColor=0; }
    public void OnArrowFired(Arrow arrow,float chargeSeconds)
    {
        if(!IsEquipped || arrow == null) return;
        int color = NextArrowColor;
        NextArrowColor=(NextArrowColor+1)%3;
        arrow.gameObject.AddComponent<CrystalArrowImpact>().Initialize(this,color,ExplosionRadius,
            profile.ChargeFraction(chargeSeconds)>=1f);
    }
    public bool TryCast()
    {
        if(!CanReleaseOrbit || StageRewardController.InputBlocked || Time.timeScale<=0f ||
            GameManager.player_current_HP<=0 || owner.on_dash || owner.skill_using) return false;
        var targets=CrystalEffects.Targets(owner.transform.position,targetRadius);
        targets.RemoveAll(e=>(CrystalEffects.TargetPoint(e)-(Vector2)owner.transform.position).sqrMagnitude>targetRadius*targetRadius);
        targets.Sort((a,b)=>((Vector2)a.transform.position-(Vector2)owner.transform.position).sqrMagnitude.CompareTo(
            ((Vector2)b.transform.position-(Vector2)owner.transform.position).sqrMagnitude));
        if(owner.chargeBow!=null) owner.chargeBow.CancelCharge();
        var e=owner.GetComponent<BowIcicleSkill>();
        var orbit=e.ActiveOrbit;
        int released=orbit.ReleaseCrystal(targets,this);
        if(released<=0)return false;
        KnockbackNearbyEnemies(e);
        readyAt=Time.time+cooldownSeconds;
        e.ConsumeOrbitForCrystal(orbit);
        RefreshHUD();
        return true;
    }
    void KnockbackNearbyEnemies(BowIcicleSkill orbitSkill)
    {
        Vector2 center=orbitSkill.transform.position+orbitSkill.centerOffset;
        float radius=Mathf.Max(.01f,orbitSkill.maxRadius);
        var pushes=new List<EnemyBowPull>();
        foreach(var enemy in CrystalEffects.Targets(center,radius))
        {
            Vector2 target=CrystalEffects.TargetPoint(enemy);
            Vector2 radial=target-center;
            if(radial.sqrMagnitude>=radius*radius)continue;
            Vector2 direction=radial.sqrMagnitude>.0001f ? radial.normalized :
                (Vector2)(Quaternion.Euler(0,0,Random.Range(0f,360f))*Vector2.right);
            Vector2 destination=(Vector2)enemy.transform.position+center+direction*radius-target;
            var push=enemy.GetComponent<EnemyBowPull>();
            if(push==null)push=enemy.gameObject.AddComponent<EnemyBowPull>();
            push.KnockbackTo(destination,.3f);
            pushes.Add(push);
        }
        for(int i=0;i<pushes.Count;i++)
            for(int j=i+1;j<pushes.Count;j++)pushes[i].IgnoreCollisionsWith(pushes[j]);
    }

    public float ExplosionRadius => owner != null && owner.GetComponent<BowExplosiveArrowSkill>() != null ?
        owner.GetComponent<BowExplosiveArrowSkill>().explosionRadius : 3.25f;
    void RefreshHUD()
    {
        if(owner==null) owner=GetComponent<Player>();
        if(hud!=null) hud.SetActive(IsActiveBow);
        if(icon!=null) icon.color=CanReleaseOrbit ? Color.white : new Color(.4f,.4f,.4f,1f);
        if(cooldownMask!=null)cooldownMask.fillAmount=cooldownSeconds>0 ? RemainingCooldown/cooldownSeconds : 0;
        if(cooldownText!=null){cooldownText.text="";cooldownText.gameObject.SetActive(false);}
    }
}
