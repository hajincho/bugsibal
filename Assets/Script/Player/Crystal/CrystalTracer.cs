using UnityEngine;

public sealed class CrystalTracer : MonoBehaviour
{
    Transform source;
    Vector3 localTip,lastTip,lastSample;
    TrailRenderer trail;
    Material sparkMaterial;
    bool sparkle,finished;
    float nextSpark;
    Color sparkTint;
    int sparkColorIndex;
    bool whiteFirst;
    public bool PhysicsTip { get; set; }
    public Vector3 Tip { get { if(source==null)return lastTip;var rb=PhysicsTip?source.GetComponent<Rigidbody2D>():null;return rb!=null?(Vector3)rb.position+Quaternion.Euler(0,0,rb.rotation)*localTip:source.TransformPoint(localTip); } }
    public void ContinueFromBounce(Vector3 corner)
    {
        if(finished || trail==null)return;
        trail.AddPosition(corner);trail.transform.position=corner;lastSample=lastTip=corner;localTip=Vector3.zero;
    }
    public TrailRenderer Trail => trail;
    public void Initialize(Transform follow,Vector3 tip,Material glow,Material white,bool sparkles=true)
    {
        source=follow;localTip=tip;sparkMaterial=white;sparkle=sparkles;
        lastTip=Tip;
        var root=new GameObject("CrystalFrontTracer",typeof(TrailRenderer));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,gameObject.scene);
        root.transform.position=lastTip;
        trail=root.GetComponent<TrailRenderer>();
        trail.sharedMaterial=glow;trail.time=.8f;trail.minVertexDistance=.025f;
        trail.startWidth=.105f;trail.endWidth=.008f;
        Color hue=glow!=null && glow.HasProperty("_TintColor") ? glow.GetColor("_TintColor") : Color.white;
        sparkTint=Color.Lerp(hue,Color.white,.25f);
        trail.startColor=new Color(hue.r,hue.g,hue.b,.95f);trail.endColor=new Color(hue.r,hue.g,hue.b,0);
        trail.numCapVertices=3;trail.numCornerVertices=2;trail.sortingOrder=25;
        trail.emitting=false;trail.Clear();trail.AddPosition(lastTip);lastSample=lastTip;trail.emitting=true;
    }
    public static Vector3 LocalFront(Transform source)
    {
        Physics2D.SyncTransforms();
        var body=source.GetComponent<Rigidbody2D>();
        Vector2 direction=body!=null && body.velocity.sqrMagnitude>.001f ? body.velocity.normalized : (Vector2)(-source.right);
        Vector3 best=source.position;float distance=float.NegativeInfinity;
        foreach(var hitbox in source.GetComponentsInChildren<Collider2D>())
        {
            var point=hitbox.ClosestPoint((Vector2)source.position+direction*100f);
            float d=Vector2.Dot(point-(Vector2)source.position,direction);
            if(d>distance){distance=d;best=point;}
        }
        if(float.IsNegativeInfinity(distance))best=source.position+(Vector3)direction*.35f;
        best=source.position+(Vector3)direction*Mathf.Max(0f,Vector2.Dot((Vector2)(best-source.position),direction));
        return source.InverseTransformPoint(best);
    }
    Color NextSparkColor()
    {
        if(sparkColorIndex%2==0)whiteFirst=Random.value<.5f;
        bool white=sparkColorIndex%2==0 ? whiteFirst : !whiteFirst;
        sparkColorIndex++;
        return white ? Color.white : sparkTint;
    }
    public void SampleTip()
    {
        if(finished || trail==null || source==null)return;
        var point=Tip;
        if((point-lastSample).sqrMagnitude>=.0004f){trail.AddPosition(point);lastSample=point;}
        trail.transform.position=point;
    }
    void LateUpdate()
    {
        if(finished || trail==null || source==null)return;
        Vector3 tip=Tip;SampleTip();
        if(sparkle && sparkMaterial!=null && Time.time>=nextSpark)
        {
            nextSpark=Time.time+Random.Range(.02f,.035f);
            var body=source.GetComponent<Rigidbody2D>();
            Vector2 movement=body!=null ? body.velocity : (Vector2)(tip-lastTip)/Mathf.Max(.001f,Time.deltaTime);
            Vector2 forward=movement.sqrMagnitude>.001f ? movement.normalized : (Vector2)(-source.right);
            Vector2 side=Vector2.Perpendicular(forward);
            int amount=Random.value<.3f?3:2;
            for(int i=0;i<amount;i++)
            {
                float sign=i<2 ? (i==0?-1f:1f) : (Random.value<.5f?-1f:1f);
                Vector2 drift=forward*(movement.magnitude*Random.Range(.45f,.7f)-Random.Range(1.5f,3.5f))+
                    side*(sign*Random.Range(2.2f,5.2f));
                drift=Quaternion.Euler(0,0,Random.Range(-7f,7f))*drift;
                CrystalSparkle.Create(tip+(Vector3)(Random.insideUnitCircle*.025f),sparkMaterial,
                    drift,Random.Range(1.078f,2.352f),Random.Range(1.44f,2.76f),NextSparkColor());
            }
        }
        lastTip=tip;
    }
    public void FinishAt(Vector3 position)
    {
        if(finished)return;finished=true;lastTip=position;
        if(trail!=null)
        {
            if((position-lastSample).sqrMagnitude>.000001f)trail.AddPosition(position);
            trail.transform.position=position;trail.emitting=false;
            Destroy(trail.gameObject,.85f);trail=null;
        }
    }
    void OnDestroy(){if(!finished)FinishAt(Tip);}
}

public sealed class CrystalSparkle : MonoBehaviour
{
    static Mesh shape;
    float began,lifetime,baseSize,spin,flickerPhase,brightness;
    Color tint;
    Vector2 velocity;
    MeshRenderer starRenderer;
    MaterialPropertyBlock block;
    void Awake(){block=new MaterialPropertyBlock();}
    public static CrystalSparkle Create(Vector3 point,Material material,Vector2 drift,float size,float seconds,Color? color=null)
    {
        if(shape==null)
        {
            shape=new Mesh{name="CrystalWhiteSpark"};
            var vertices=new Vector3[9];var triangles=new int[24];
            for(int i=0;i<8;i++)
            {
                float angle=i*Mathf.PI*.25f;
                vertices[i+1]=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*(i%2==0?.1f:.018f);
                triangles[i*3]=0;triangles[i*3+1]=i+1;triangles[i*3+2]=(i+1)%8+1;
            }
            shape.vertices=vertices;shape.triangles=triangles;shape.uv=new Vector2[9];
            var colors=new Color[9];for(int i=0;i<9;i++)colors[i]=Color.white;
            shape.colors=colors;shape.RecalculateBounds();
        }
        var root=new GameObject("CrystalWhiteEmber",typeof(MeshFilter),typeof(MeshRenderer));
        root.transform.position=point;root.transform.rotation=Quaternion.Euler(0,0,Random.Range(0,90));
        root.GetComponent<MeshFilter>().sharedMesh=shape;
        var ember=root.AddComponent<CrystalSparkle>();
        ember.began=Time.time;ember.lifetime=seconds;ember.velocity=drift;ember.baseSize=size;
        ember.tint=color ?? Color.white;
        Color light=material!=null && material.HasProperty("_TintColor") ? material.GetColor("_TintColor") : Color.white;
        ember.brightness=Mathf.Max(light.r,Mathf.Max(light.g,light.b));
        ember.spin=Random.Range(-180f,180f);ember.flickerPhase=Random.Range(0,Mathf.PI*2);
        ember.starRenderer=root.GetComponent<MeshRenderer>();ember.starRenderer.sharedMaterial=material;
        ember.starRenderer.sortingOrder=29;root.transform.localScale=Vector3.one*size;
        return ember;
    }
    void Update()
    {
        float t=(Time.time-began)/Mathf.Max(.01f,lifetime);
        if(t>=1){Destroy(gameObject);return;}
        float dt=Time.deltaTime;
        velocity+=Vector2.down*(.7f*dt);
        velocity*=Mathf.Exp(-1.3f*dt);
        transform.position+=(Vector3)velocity*dt;
        transform.Rotate(0,0,spin*dt);
        float flicker=.9f+.1f*Mathf.Sin(t*18f+flickerPhase);
        transform.localScale=Vector3.one*(baseSize*(1-t)*flicker);
        block.SetColor("_TintColor",new Color(tint.r*brightness,tint.g*brightness,tint.b*brightness,1f));
        block.SetColor("_RendererColor",new Color(tint.r,tint.g,tint.b,Mathf.Pow(1-t,1.4f)*flicker));
        starRenderer.SetPropertyBlock(block);
    }
}
