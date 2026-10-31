using UnityEngine;

// A transparent glass rim with drifting pearlescent highlights, following the orb pulse.
public sealed class LunarSoulAeroAura : MonoBehaviour
{
    const int Segments=96, Bands=10;
    readonly Vector3[] vertices=new Vector3[(Segments+1)*(Bands+1)];
    readonly Color[] colors=new Color[(Segments+1)*(Bands+1)];
    Mesh mesh;
    Material material;
    float radius,phase;

    public static void Attach(Transform orb,Sprite sprite,Material silver)
    {
        if(sprite==null || silver==null)return;
        var go=new GameObject("LunarSoulAeroRim");
        go.transform.SetParent(orb,false);
        var aura=go.AddComponent<LunarSoulAeroAura>();
        // Parent orb shrinks by 15%; this gives a total aura reduction of 20%.
        aura.radius=sprite.bounds.size.y*.368f;
        aura.phase=Random.value*Mathf.PI*2;
        aura.mesh=new Mesh{name="Soul Aero glass rim"};aura.mesh.MarkDynamic();
        aura.material=new Material(silver);
        aura.material.mainTexture=Texture2D.whiteTexture;
        aura.material.SetColor("_Color",Color.white);
        go.AddComponent<MeshFilter>().sharedMesh=aura.mesh;
        var renderer=go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial=aura.material;renderer.sortingOrder=29;
        var triangles=new int[Segments*Bands*6];
        for(int band=0;band<Bands;band++)for(int j=0;j<Segments;j++)
        {
            int v=band*(Segments+1)+j,k=(band*Segments+j)*6;
            triangles[k]=v;triangles[k+1]=v+1;triangles[k+2]=v+Segments+1;
            triangles[k+3]=v+1;triangles[k+4]=v+Segments+2;triangles[k+5]=v+Segments+1;
        }
        aura.Draw();aura.mesh.triangles=triangles;
    }
    void LateUpdate(){Draw();}
    void Draw()
    {
        float time=Time.time*.65f+phase;
        for(int j=0;j<=Segments;j++)
        {
            float angle=j*Mathf.PI*2/Segments;
            float ripple=Mathf.Sin(angle*3-time)*.014f+Mathf.Sin(angle*5+time*.7f)*.008f;
            float gleam=Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle-time*.8f)),12);
            float ribbon=.5f+.5f*Mathf.Sin(angle*2+time);
            Color hue=Color.Lerp(new Color(.43f,.85f,1),new Color(.82f,.69f,1),ribbon);
            // Keep the moving cool hues, softened toward pearlescent silver.
            hue=Color.Lerp(hue,new Color(.9f,.92f,.95f),.3f);
            hue=Color.Lerp(hue,Color.white,.35f+gleam*.65f);
            for(int band=0;band<=Bands;band++)
            {
                float u=band/(float)Bands;
                float r=radius*(.82f+u*.35f+ripple*Mathf.Sin(u*Mathf.PI));
                int index=band*(Segments+1)+j;
                vertices[index]=new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,0);
                float glow=Mathf.Exp(-Mathf.Pow((u-.49f)*4,2))*.22f;
                float edge=Mathf.Exp(-Mathf.Pow((u-.5f-ripple*2)*17,2))*(.3f+gleam*.43f);
                float sheen=Mathf.Exp(-Mathf.Pow((u-(.3f+.12f*Mathf.Sin(angle+time)))*12,2))*.15f;
                hue.a=Mathf.Clamp01((glow+edge+sheen)*Mathf.Sin(u*Mathf.PI));
                colors[index]=hue;
            }
        }
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
    }
    void OnDestroy()
    {
        if(mesh!=null)Destroy(mesh);
        if(material!=null)Destroy(material);
    }
}
