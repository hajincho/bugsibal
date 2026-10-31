using UnityEngine;
using System.Collections.Generic;

// A visual-only copy of the player's animated skeleton. No Player, enemy AI or gameplay scripts are cloned.
[DefaultExecutionOrder(22000)]
public sealed class LunarEchoRig : MonoBehaviour
{
    LunarClone owner;
    JusticeCharacterSkin skin;
    Animator animator;
    Transform rig,compactRoot,headRoot,weaponBone;
    readonly Dictionary<Transform,Transform> bones=new Dictionary<Transform,Transform>();
    readonly List<SpriteRenderer> renderers=new List<SpriteRenderer>();
    readonly List<Sprite> sprites=new List<Sprite>();
    Transform weapon;
    float attackUntil;
    float referenceScale,rootScale;
    public Animator Animator => animator;
    public bool IsAttacking => Time.time<attackUntil;
    public Vector3 Muzzle => weapon!=null?weapon.position:owner.AimPoint;
    public void Initialize(LunarClone clone)
    {
        owner=clone;skin=clone.Source.skin;
        var source=clone.Source.Owner;
        referenceScale=Mathf.Abs(source.transform.lossyScale.x);rootScale=Mathf.Abs(source.transform.Find("Root").localScale.x);
        rig=new GameObject("EchoVisualRig").transform;rig.SetParent(transform,false);rig.localScale=Vector3.one*referenceScale;
        var skeleton=CopySkeleton(source.transform.Find("Root"),rig);skeleton.localRotation=Quaternion.identity;skeleton.localPosition=Vector3.zero;
        compactRoot=Map(skin.visualScaleRoot);headRoot=Map(skin.headMotionRoot);
        animator=rig.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController=source.GetComponent<Animator>().runtimeAnimatorController;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
        animator.Rebind();animator.Update(0);
        animator.SetInteger("Attack",-1);animator.SetInteger("Attacked",-1);animator.SetBool("Run",false);
        foreach(var part in skin.parts)
        {
            var sprite=Sprite.Create(skin.partsTexture,part.rectangle,part.pivot,100,0,SpriteMeshType.FullRect);sprites.Add(sprite);
            var go=new GameObject("Echo_"+part.name,typeof(SpriteRenderer));go.transform.SetParent(rig,false);
            var sr=go.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.flipX=part.flipX;sr.sortingOrder=part.sortingOrder;
            sr.color=new Color(.62f,.64f,.67f,.88f);renderers.Add(sr);
        }
        weaponBone=owner.Archer?Map(source.chargeBow.transform):
            rig.Find("Root/BodySet/P_Body/ArmSet/ArmR/P_RArm/P_Weapon");
        weapon=new GameObject(owner.Archer?"EchoBow":"EchoSword",typeof(SpriteRenderer)).transform;weapon.SetParent(transform,false);
        var w=weapon.GetComponent<SpriteRenderer>();w.sprite=owner.Archer?clone.Source.echoBowSprite:clone.Source.swordSprite;
        w.sharedMaterial=clone.Source.echoWeaponMaterial;w.color=new Color(.62f,.64f,.67f,.9f);w.sortingOrder=5;
        LateUpdate();
    }
    Transform CopySkeleton(Transform source,Transform parent)
    {
        var copy=new GameObject(source.name).transform;copy.SetParent(parent,false);
        copy.localPosition=source.localPosition;copy.localRotation=source.localRotation;copy.localScale=source.localScale;
        bones[source]=copy;
        foreach(Transform child in source)
        {
            // Runtime appearance pieces and procedural weapons aren't skeleton nodes.
            if(child.name.StartsWith("JusticeSkin_")||child.name.StartsWith("LoveSkin_")||child.name.StartsWith("ArtemisiaSkin_")||child.name.Contains("DrawVisual"))continue;
            CopySkeleton(child,copy);
        }
        return copy;
    }
    Transform Map(Transform source){Transform result;return source!=null && bones.TryGetValue(source,out result)?result:null;}
    public void SetMoving(bool moving){if(animator!=null)animator.SetBool("Run",moving && Time.time>=attackUntil);}
    public void BeginAttack()
    {
        attackUntil=Time.time+(owner.Archer?.84f:.48f);
        animator.SetBool("Run",false);animator.SetInteger("Attack",owner.Archer?1:0);
    }
    void Update(){if(animator!=null && Time.time>=attackUntil)animator.SetInteger("Attack",-1);}
    void LateUpdate()
    {
        if(skin==null || compactRoot==null)return;
        bool moving=animator.GetBool("Run");
        Vector3 origin=compactRoot.position;
        for(int i=0;i<renderers.Count;i++)
        {
            var sr=renderers[i];var p=skin.parts[i];var bone=Map(p.bone);
            Vector3 point=bone.TransformPoint(p.localPosition);Quaternion rotation=bone.rotation;
            if(p.compactLegRoot!=null)
            {
                var root=Map(p.compactLegRoot);Vector3 animated=root.InverseTransformPoint(bone.position);Vector3 socket=p.restPosition+p.localPosition;
                socket.x+=(animated.x-p.sourceRestPosition.x)*.2f;socket.y+=(animated.y-p.sourceRestPosition.y)*.6f;
                if(p.hipAnchor!=null)socket+=root.InverseTransformPoint(Map(p.hipAnchor).position)-p.hipRestPosition;
                point=root.TransformPoint(socket);
                float angle=Mathf.DeltaAngle(0,(Quaternion.Inverse(root.rotation)*bone.rotation).eulerAngles.z);
                rotation=root.rotation*Quaternion.Euler(0,0,angle*.45f);
            }
            else if(p.shoulderAnchor!=null)
            {
                point=Map(p.shoulderAnchor).TransformPoint(p.shoulderPosition);
                Vector3 shoulder=bone.InverseTransformPoint(point);Vector2 reach=p.handPosition-(Vector2)shoulder;
                rotation=bone.rotation*Quaternion.Euler(0,0,Vector2.SignedAngle(p.spriteHandVector,reach));
            }
            else rotation*=Quaternion.Euler(0,0,Mathf.Sin(Time.time*(moving?11:3.5f)+i*.9f)*p.swayDegrees*(moving?1:.3f));
            sr.transform.position=origin+(point-origin)*skin.visualScale;
            sr.transform.rotation=rotation;
            float scale=p.height/Mathf.Max(.001f,sr.sprite.bounds.size.y)*skin.visualScale*rootScale;
            // Sprites live under one uniform root, so bone scale/rotation cannot shear the image.
            sr.transform.localScale=new Vector3(scale*p.widthScale,scale,scale);
        }
        if(moving && headRoot!=null)
        {
            Vector3 center=headRoot.InverseTransformPoint(renderers[0].bounds.center);
            Vector3 correction=headRoot.TransformVector(new Vector3((skin.idleHeadCenter.x-center.x)*(1-skin.runningHeadMotion),0,0));
            foreach(int i in new[]{0,6,7})renderers[i].transform.position+=correction;
        }
        if(weaponBone!=null)
        {
            Vector3 point=weaponBone.position;
            weapon.position=origin+(point-origin)*skin.visualScale;
            weapon.rotation=weaponBone.rotation;
            var sprite=weapon.GetComponent<SpriteRenderer>().sprite;
            float size=(owner.Archer?1.35f:1.3f)/Mathf.Max(.01f,sprite.bounds.size.y);
            weapon.localScale=Vector3.one*size;
        }
    }
    void OnDestroy(){foreach(var sprite in sprites)if(sprite!=null)Destroy(sprite);}
}
