using UnityEngine;

// Animated geometry keeps the lunar arcs sharp at every explosion size.
public sealed class LunarQMoonburst : MonoBehaviour
{
    Mesh mesh;
    Material material;
    float born, radius;
    const float Lifetime=.95f;

    public static void Create(Vector3 position,float radius,Material silver,Material sparkle,bool fullCharge=true)
    {
        var root=new GameObject("ArtemisiaQMoonburst");
        root.transform.position=position;
        var effect=root.AddComponent<LunarQMoonburst>();
        effect.radius=radius;effect.born=Time.time;
        effect.mesh=new Mesh();effect.mesh.name="Lunar Q crescent arcs";
        root.AddComponent<MeshFilter>().sharedMesh=effect.mesh;
        var renderer=root.AddComponent<MeshRenderer>();
        effect.material=new Material(silver);effect.material.mainTexture=Texture2D.whiteTexture;
        renderer.sharedMaterial=effect.material;renderer.sortingOrder=30;
        LunarArrowImpact.BurstSparkles(sparkle,position,radius,fullCharge);
        effect.Render(0);Destroy(root,1.3f);
    }
    void Update()
    {
        float t=(Time.time-born)/Lifetime;
        if(t>=1){Destroy(gameObject);return;}
        Render(t);
    }
    void Render(float t)
    {
        const int Steps=40,Arcs=11;
        var vertices=new Vector3[Arcs*(Steps+1)*2];
        var colors=new Color[vertices.Length];
        var indices=new int[Arcs*Steps*6];
        float ease=1-Mathf.Pow(1-t,3);
        for(int arc=0;arc<Arcs;arc++)
        {
            bool ring=arc==3 || arc==4;
            bool satellite=arc>=5;
            float r=radius*(.15f+ease*.85f)*(1-Mathf.Min(arc,2)*.19f);
            if(ring)r=radius*(.2f+ease*.8f)*(arc==3?1.03f:.48f);
            if(satellite)r=radius*.115f*(.4f+ease*.6f);
            float rotation=arc*2.0944f+t*(arc==1?-.55f:.55f);
            float orbit=(arc-5)*Mathf.PI/3+t*.45f;
            Vector3 center=satellite?new Vector3(Mathf.Cos(orbit),Mathf.Sin(orbit),0)*radius*(.12f+ease*.69f):Vector3.zero;
            for(int j=0;j<=Steps;j++)
            {
                float u=j/(float)Steps;
                float angle=ring?u*Mathf.PI*2:rotation+Mathf.Lerp(-2.3f,2.3f,u);
                Vector3 d=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                float width=radius*(satellite?.055f:.11f)*Mathf.Sin(u*Mathf.PI)*(1-t);
                if(ring)width=radius*.012f*(1-t);
                int v=(arc*(Steps+1)+j)*2;
                vertices[v]=center+d*r;vertices[v+1]=center+d*(r-width);
                var c=arc==1?new Color(.75f,.9f,1f):new Color(.98f,.98f,1f);
                c.a=(1-t)*(ring?.42f:.9f);colors[v]=colors[v+1]=c;
                if(j==Steps)continue;
                int k=(arc*Steps+j)*6;
                indices[k]=v;indices[k+1]=v+2;indices[k+2]=v+1;
                indices[k+3]=v+1;indices[k+4]=v+2;indices[k+5]=v+3;
            }
        }
        mesh.Clear();mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=indices;mesh.RecalculateBounds();
    }
    void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
