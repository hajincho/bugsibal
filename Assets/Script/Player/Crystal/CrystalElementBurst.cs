using UnityEngine;
using System.Collections.Generic;

// Visual only: damage and status effects remain owned by CrystalEffects.
public sealed class CrystalElementBurst : MonoBehaviour
{
    sealed class Bolt
    {
        public Transform transform;
        public MeshRenderer renderer;
        public Vector2 direction;
        public float delay,speed,phase;
    }
    static readonly Dictionary<Sprite,Mesh[]> sectors=new Dictionary<Sprite,Mesh[]>();
    readonly List<Bolt> bolts=new List<Bolt>();
    SpriteRenderer image;
    Material boltMaterial;
    MaterialPropertyBlock block;
    float began,diameter,radius;
    bool electric;
    public float Duration => electric ? .7f : .5f;

    public void Initialize(Sprite sprite,float effectRadius,int color)
    {
        image=gameObject.AddComponent<SpriteRenderer>();
        image.sprite=sprite;image.sortingOrder=9;
        electric=color==2;began=Time.time;radius=effectRadius;
        diameter=2f*radius/Mathf.Max(.01f,Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y));
        transform.rotation=Quaternion.Euler(0,0,Random.Range(0f,electric?360f:60f));
        if(electric)CreateBolts(sprite);
        RenderAt(0f);
    }
    void CreateBolts(Sprite sprite)
    {
        const int count=12;
        Mesh[] meshes;
        if(!sectors.TryGetValue(sprite,out meshes))
        {
            meshes=new Mesh[count];
            Rect uv=sprite.rect;
            for(int i=0;i<count;i++)
            {
                const int subdivisions=4;
                var vertices=new Vector3[subdivisions+2];
                var coords=new Vector2[vertices.Length];
                var colors=new Color[vertices.Length];
                var triangles=new int[subdivisions*3];
                coords[0]=new Vector2(uv.center.x/sprite.texture.width,uv.center.y/sprite.texture.height);
                colors[0]=Color.white;
                for(int j=0;j<=subdivisions;j++)
                {
                    float angle=(i+j/(float)subdivisions)*Mathf.PI*2f/count;
                    Vector2 point=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.707107f;
                    vertices[j+1]=point;
                    coords[j+1]=new Vector2((uv.x+(point.x+.5f)*uv.width)/sprite.texture.width,
                        (uv.y+(point.y+.5f)*uv.height)/sprite.texture.height);
                    colors[j+1]=Color.white;
                    if(j<subdivisions){triangles[j*3]=0;triangles[j*3+1]=j+1;triangles[j*3+2]=j+2;}
                }
                var mesh=new Mesh{name="CrystalElectricSector"+i};
                mesh.vertices=vertices;mesh.uv=coords;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
                meshes[i]=mesh;
            }
            sectors[sprite]=meshes;
        }
        boltMaterial=new Material(Shader.Find("Sprites/Default"));
        boltMaterial.mainTexture=sprite.texture;
        block=new MaterialPropertyBlock();
        for(int i=0;i<count;i++)
        {
            float angle=(i+.5f)*Mathf.PI*2f/count;
            var child=new GameObject("OutwardLightning"+i,typeof(MeshFilter),typeof(MeshRenderer));
            child.transform.SetParent(transform,false);
            child.GetComponent<MeshFilter>().sharedMesh=meshes[i];
            var renderer=child.GetComponent<MeshRenderer>();renderer.sharedMaterial=boltMaterial;renderer.sortingOrder=10;
            bolts.Add(new Bolt{transform=child.transform,renderer=renderer,
                direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle)),
                delay=Random.Range(0f,.055f),speed=Random.Range(.85f,1.15f),phase=Random.Range(0f,Mathf.PI*2f)});
        }
    }
    public void RenderAt(float seconds)
    {
        if(image==null)return;
        float t=Mathf.Clamp01(seconds/Duration);
        if(!electric)
        {
            float expansion=1f-Mathf.Pow(1f-Mathf.Clamp01(t/.55f),3f);
            transform.localScale=Vector3.one*(diameter*Mathf.Lerp(.15f,1f,expansion));
            float alpha=.5f*(1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.2f,1f,t)));
            image.color=new Color(1f,1f,1f,alpha);
            return;
        }
        transform.localScale=Vector3.one;
        image.enabled=false;
        foreach(var bolt in bolts)
        {
            float age=Mathf.Clamp01((seconds-bolt.delay)/.6f);
            bool visible=seconds>=bolt.delay && age<1f;
            bolt.renderer.enabled=visible;
            if(!visible)continue;
            float travel=age*bolt.speed;
            float scale=radius*2f*Mathf.Lerp(.12f,.72f,Mathf.Clamp01(travel*2.3f));
            bolt.transform.localScale=Vector3.one*scale;
            bolt.transform.localPosition=bolt.direction*(radius*.32f*travel);
            float flicker=.72f+.28f*Mathf.Sin(age*55f+bolt.phase);
            float fade=1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.3f,1f,age));
            block.SetColor("_Color",new Color(1f,1f,1f,fade*flicker));
            bolt.renderer.SetPropertyBlock(block);
        }
    }
    void Update()
    {
        float seconds=Time.time-began;
        RenderAt(seconds);
        if(!electric)transform.Rotate(0,0,12f*Time.deltaTime);
        if(seconds>=Duration)Destroy(gameObject);
    }
    void OnDestroy(){if(boltMaterial!=null)Destroy(boltMaterial);}
}
