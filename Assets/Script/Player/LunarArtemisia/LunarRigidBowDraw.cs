using UnityEngine;

// Keeps the bow art rigid. Only the separate string's center point moves toward the archer.
[DefaultExecutionOrder(30000)]
public sealed class LunarRigidBowDraw : MonoBehaviour
{
    GameObject visual;
    SpriteRenderer body,source;
    LineRenderer[] strings;
    Material stringMaterial;
    BowChargeSettings profile;
    Player owner;
    float draw,recoil,releaseDraw,uniformScale;
    public GameObject Visual => visual;
    public float DrawFraction => draw;
    public void Initialize(BowChargeSettings settings,SpriteRenderer sprite)
    {
        Clear();profile=settings;source=sprite;owner=GetComponentInParent<Player>();
        uniformScale=Mathf.Abs(source.transform.lossyScale.x)*profile.heldVisualScale;
        visual=new GameObject("LunarRigidBowVisual",typeof(SpriteRenderer));
        if(owner!=null)visual.transform.SetParent(owner.transform,false);
        else UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual,gameObject.scene);
        body=visual.GetComponent<SpriteRenderer>();body.sprite=profile.idleSprite;body.sharedMaterial=source.sharedMaterial;
        body.sortingLayerID=source.sortingLayerID;body.sortingOrder=source.sortingOrder;
        stringMaterial=new Material(Shader.Find("Sprites/Default"));strings=new LineRenderer[2];
        for(int i=0;i<strings.Length;i++)
        {
            var go=new GameObject("LunarBowString"+i,typeof(LineRenderer));go.transform.SetParent(visual.transform,false);
            var line=go.GetComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;line.sharedMaterial=stringMaterial;
            line.startWidth=line.endWidth=.0035f;line.startColor=line.endColor=new Color(.81f,.87f,.94f,.9f);
            line.numCapVertices=1;line.numCornerVertices=1;line.sortingLayerID=body.sortingLayerID;line.sortingOrder=body.sortingOrder+1;strings[i]=line;
        }
        source.enabled=false;SetDraw(0);LateUpdate();
    }
    Vector3 SpritePoint(Vector2 normalized)
    {
        var sprite=body.sprite;
        return new Vector3((normalized.x*sprite.rect.width-sprite.pivot.x)/sprite.pixelsPerUnit,
            (normalized.y*sprite.rect.height-sprite.pivot.y)/sprite.pixelsPerUnit,0);
    }
    public void SetDraw(float amount){draw=Mathf.Clamp01(amount);recoil=0;UpdateString();}
    public void Release(){releaseDraw=draw;recoil=.16f;}
    public void ResetDraw(){draw=0;recoil=0;UpdateString();}
    void UpdateString()
    {
        if(body==null || strings==null)return;
        Vector3 lower=SpritePoint(new Vector2(.255f,.10f)),upper=SpritePoint(new Vector2(.83f,.9f));
        Vector3 along=(upper-lower).normalized;Vector3 pull=new Vector3(along.y,-along.x,0);
        for(int i=0;i<strings.Length;i++)
        {
            Vector3 offset=pull*((i-.5f)*.005f);
            strings[i].SetPosition(0,lower+offset);
            strings[i].SetPosition(1,(lower+upper)*.5f+offset+pull*(body.sprite.bounds.size.x*.13f*draw));
            strings[i].SetPosition(2,upper+offset);
        }
    }
    void LateUpdate()
    {
        if(visual==null || source==null)return;
        bool visible=isActiveAndEnabled && source.gameObject.activeInHierarchy && (owner==null||owner.weapon_mode==0);
        visual.SetActive(visible);if(!visible)return;
        source.enabled=false;
        if(recoil>0){recoil=Mathf.Max(0,recoil-Time.deltaTime);float t=1-recoil/.16f;draw=releaseDraw*Mathf.Cos(t*Mathf.PI*2)*(1-t);UpdateString();}
        Vector3 position=source.transform.position;
        var skin=owner!=null?owner.GetComponent<LunarBowSkill>():null;
        if(skin!=null && skin.IsEquipped && skin.skin.visualScaleRoot!=null)
        {Vector3 origin=skin.skin.visualScaleRoot.position;position=origin+(position-origin)*skin.skin.visualScale;}
        visual.transform.position=position;visual.transform.rotation=source.transform.rotation;
        var scale=source.transform.lossyScale;
        var parentScale=visual.transform.parent!=null?visual.transform.parent.lossyScale:Vector3.one;
        visual.transform.localScale=new Vector3(uniformScale*Mathf.Sign(scale.x)/parentScale.x,uniformScale*Mathf.Sign(scale.y)/parentScale.y,1);
        body.color=source.color;
    }
    void OnDisable(){if(visual!=null)visual.SetActive(false);}
    public void Clear()
    {
        if(visual!=null){visual.SetActive(false);Destroy(visual);}if(stringMaterial!=null)Destroy(stringMaterial);
        visual=null;body=null;strings=null;profile=null;if(source!=null)source.enabled=true;
    }
    void OnDestroy(){Clear();}
}
