using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class JusticeSkinPart
{
    public string name;
    public Rect rectangle;
    public Vector2 pivot=new Vector2(.5f,.5f);
    public Transform bone;
    public Vector3 localPosition;
    public float height=.3f;
    public float widthScale=1f;
    public bool flipX;
    public Transform compactLegRoot;
    public Transform hipAnchor;
    public Vector3 hipRestPosition;
    public Vector3 sourceRestPosition;
    public Vector3 restPosition;
    public int sortingOrder;
    public float swayDegrees;
    public Transform shoulderAnchor;
    public Vector3 shoulderPosition;
    public Vector2 handPosition;
    public Vector2 spriteHandVector;
}

// Keeps the original gameplay rig and animations; changes only its visible skin.
[DefaultExecutionOrder(20000)]
[RequireComponent(typeof(Player))]
public sealed class JusticeCharacterSkin : MonoBehaviour
{
    public BowChargeSettings requiredBow;
    public string skinPrefix="JusticeSkin";
    public Texture2D partsTexture;
    public JusticeSkinPart[] parts;
    public SpriteRenderer[] originalRenderers;
    public bool IsTransformed { get; private set; }
    public Vector3 VisualHeadCenter => skinRenderers.Count>0 && skinRenderers[0]!=null ? skinRenderers[0].bounds.center : transform.position+Vector3.up*1.6f;
    public Transform headMotionRoot;
    public Transform visualScaleRoot;
    [Min(.01f)] public float visualScale=1f;
    public Vector3 idleHeadCenter;
    [Range(0f,1f)] public float runningHeadMotion=.5f;
    readonly List<SpriteRenderer> skinRenderers=new List<SpriteRenderer>();
    readonly List<Sprite> generatedSprites=new List<Sprite>();
    bool[] originalHidden;
    Player owner;
    Animator animator;
    void Awake()
    {
        owner=GetComponent<Player>();animator=GetComponent<Animator>();
        originalHidden=new bool[originalRenderers.Length];
        for(int i=0;i<originalRenderers.Length;i++)
            if(originalRenderers[i]!=null)originalHidden[i]=originalRenderers[i].forceRenderingOff;
        foreach(var other in GetComponents<JusticeCharacterSkin>())
            if(other!=this && other.originalHidden!=null && other.originalHidden.Length==originalHidden.Length)
            { originalHidden=(bool[])other.originalHidden.Clone();break; }
        foreach(var part in parts)
        {
            var sprite=Sprite.Create(partsTexture,part.rectangle,part.pivot,100f,0,SpriteMeshType.FullRect);
            sprite.name=skinPrefix+"_"+part.name;generatedSprites.Add(sprite);
            var root=new GameObject(skinPrefix+"_"+part.name);
            root.transform.SetParent(part.bone,false);root.transform.localPosition=part.localPosition;
            float scale=part.height/Mathf.Max(.001f,sprite.bounds.size.y);
            root.transform.localScale=new Vector3(scale*part.widthScale,scale,scale);
            var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=sprite;
            renderer.flipX=part.flipX;
            renderer.sortingOrder=part.sortingOrder;renderer.enabled=false;skinRenderers.Add(renderer);
        }
    }
    void LateUpdate()
    {
        Refresh();
        if(!IsTransformed)return;
        bool moving=animator!=null && animator.GetBool("Run");
        float motion=moving ? 1f : .3f;
        for(int i=0;i<skinRenderers.Count;i++)
        {
            var renderer=skinRenderers[i];
            var part=parts[i];
            renderer.transform.localPosition=part.localPosition;
            float fixedScale=part.height/Mathf.Max(.001f,renderer.sprite.bounds.size.y);
            renderer.transform.localScale=new Vector3(fixedScale*part.widthScale,fixedScale,fixedScale);
            if(part.compactLegRoot!=null)
            {
                Vector3 animated=part.compactLegRoot.InverseTransformPoint(part.bone.position);
                Vector3 socket=part.restPosition;
                socket.x+=(animated.x-part.sourceRestPosition.x)*.2f;
                socket.y+=(animated.y-part.sourceRestPosition.y)*.6f;
                socket+=part.localPosition;
                if(part.hipAnchor!=null)
                    socket+=part.compactLegRoot.InverseTransformPoint(part.hipAnchor.position)-part.hipRestPosition;
                renderer.transform.position=part.compactLegRoot.TransformPoint(socket);
                float angle=Mathf.DeltaAngle(0f,(Quaternion.Inverse(part.compactLegRoot.rotation)*part.bone.rotation).eulerAngles.z);
                renderer.transform.rotation=part.compactLegRoot.rotation*Quaternion.Euler(0,0,angle*.45f);
            }
            else if(part.shoulderAnchor!=null)
            {
                Vector3 shoulder=part.bone.InverseTransformPoint(part.shoulderAnchor.TransformPoint(part.shoulderPosition));
                Vector2 reach=part.handPosition-(Vector2)shoulder;
                renderer.transform.localPosition=shoulder;
                renderer.transform.localRotation=Quaternion.Euler(0,0,Vector2.SignedAngle(part.spriteHandVector,reach));

            }
            else renderer.transform.localRotation=Quaternion.Euler(0,0,
                Mathf.Sin(Time.time*(moving?11f:3.5f)+i*.9f)*part.swayDegrees*motion);
            renderer.color=owner!=null && owner.IsDamageFlashing ?
                new Color(1f,.72f,.72f,1f) : Color.white;
        }
        if(moving)ReduceRunningHeadMotion();
        ApplyVisualScale();
    }
    void ApplyVisualScale()
    {
        if(visualScaleRoot==null)return;
        Vector3 origin=visualScaleRoot.position;
        foreach(var renderer in skinRenderers)
        {
            renderer.transform.position=origin+(renderer.transform.position-origin)*visualScale;
            renderer.transform.localScale*=visualScale;
        }
        if(owner.chargeBow!=null && owner.EquippedBowItem!=null &&
            owner.EquippedBowItem.bowSettings==requiredBow)
        {
            var visual=owner.chargeBow.transform.Find("LongbowDrawVisual");
            if(visual!=null)visual.position=origin+(owner.chargeBow.transform.position-origin)*visualScale;
        }
    }
    void ReduceRunningHeadMotion()
    {
        if(headMotionRoot==null || skinRenderers.Count<8)return;
        Vector3 current=headMotionRoot.InverseTransformPoint(skinRenderers[0].bounds.center);
        Vector3 correction=headMotionRoot.TransformVector(new Vector3(
            (idleHeadCenter.x-current.x)*(1f-runningHeadMotion),0,0));
        skinRenderers[0].transform.position+=correction;
        skinRenderers[6].transform.position+=correction;
        skinRenderers[7].transform.position+=correction;
    }
    public void Refresh()
    {
        bool active=owner!=null && owner.EquippedBowItem!=null &&
            owner.EquippedBowItem.bowSettings==requiredBow;
        if(active==IsTransformed)return;
        IsTransformed=active;
        ApplyOriginalVisibility();
        foreach(var renderer in skinRenderers)if(renderer!=null)renderer.enabled=active;
    }
    void ApplyOriginalVisibility()
    {
        bool hide=false;
        if(owner!=null && owner.EquippedBowItem!=null)
            foreach(var skin in GetComponents<JusticeCharacterSkin>())
                if(skin.isActiveAndEnabled && skin.requiredBow==owner.EquippedBowItem.bowSettings)
                {hide=true;break;}
        for(int i=0;i<originalRenderers.Length;i++)
            if(originalRenderers[i]!=null)originalRenderers[i].forceRenderingOff=hide || originalHidden[i];
    }
    void OnDisable()
    {
        if(originalHidden==null)return;
        IsTransformed=false;
        ApplyOriginalVisibility();
        foreach(var renderer in skinRenderers)if(renderer!=null)renderer.enabled=false;
    }
    void OnDestroy(){foreach(var sprite in generatedSprites)if(sprite!=null)Destroy(sprite);}
}
