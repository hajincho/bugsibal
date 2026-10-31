using UnityEngine;

// Deform a UV-preserving mesh of the supplied bow sprite instead of replacing its art.
public sealed class BowDrawMotion : MonoBehaviour
{
    const int Segments = 12;
    SpriteRenderer source;
    GameObject visual;
    Mesh mesh;
    Material material;
    Vector3[] restVertices;
    Vector3[] vertices;
    float halfLength, bendDistance, lastDraw, recoilTime;
    bool activeMotion;
    LunarRigidBowDraw rigidDraw;

    public void Apply(BowChargeSettings profile, SpriteRenderer renderer)
    {
        ClearVisual();
        source = renderer;
        if(profile!=null && profile.rigidStringDraw && source!=null)
        {rigidDraw=GetComponent<LunarRigidBowDraw>();if(rigidDraw==null)rigidDraw=gameObject.AddComponent<LunarRigidBowDraw>();rigidDraw.Initialize(profile,source);return;}
        activeMotion = profile != null && profile.proceduralDrawMotion && source != null && profile.idleSprite != null;
        if (!activeMotion) return;
        var sprite = profile.idleSprite;
        int edge = Segments + 1;
        restVertices = new Vector3[edge * edge];
        vertices = new Vector3[restVertices.Length];
        var uv = new Vector2[restVertices.Length];
        var colors = new Color[restVertices.Length];
        var triangles = new int[Segments * Segments * 6];
        var rect = sprite.rect;
        var textureRect = sprite.textureRect;
        halfLength = Mathf.Max(.01f, (sprite.bounds.size.x + sprite.bounds.size.y) * .3535534f);
        bendDistance = sprite.bounds.size.x * .18f;
        for (int y = 0; y < edge; y++)
            for (int x = 0; x < edge; x++)
            {
                int index = y * edge + x;
                float u = (float)x / Segments, v = (float)y / Segments;
                restVertices[index] = new Vector3((u * rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
                    (v * rect.height - sprite.pivot.y) / sprite.pixelsPerUnit, 0);
                uv[index] = new Vector2((textureRect.x + u * textureRect.width) / sprite.texture.width,
                    (textureRect.y + v * textureRect.height) / sprite.texture.height);
                colors[index] = Color.white;
            }
        int cursor = 0;
        for (int y = 0; y < Segments; y++)
            for (int x = 0; x < Segments; x++)
            {
                int a = y * edge + x, b = a + 1, c = a + edge, d = c + 1;
                triangles[cursor++] = a; triangles[cursor++] = c; triangles[cursor++] = b;
                triangles[cursor++] = b; triangles[cursor++] = c; triangles[cursor++] = d;
            }
        mesh = new Mesh { name = "LongbowDrawMesh" };
        mesh.MarkDynamic(); mesh.vertices = restVertices; mesh.uv = uv; mesh.colors = colors; mesh.triangles = triangles;
        material = new Material(source.sharedMaterial) { name = "LongbowDrawMaterial" };
        material.mainTexture = sprite.texture;
        material.color = source.color;
        visual = new GameObject("LongbowDrawVisual", typeof(MeshFilter), typeof(MeshRenderer));
        visual.transform.SetParent(transform, false);
        visual.transform.localScale=Vector3.one*Mathf.Max(.01f,profile.heldVisualScale);
        visual.layer = gameObject.layer;
        visual.GetComponent<MeshFilter>().sharedMesh = mesh;
        var meshRenderer = visual.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.sortingLayerID = source.sortingLayerID;
        meshRenderer.sortingOrder = source.sortingOrder;
        source.enabled = false;
        SetDraw(0f);
    }

    public void SetDraw(float fraction)
    {
        if(rigidDraw!=null){rigidDraw.SetDraw(fraction);return;}
        if (!activeMotion || mesh == null) return;
        recoilTime = 0f;
        lastDraw = Mathf.Clamp01(fraction);
        Deform(lastDraw);
    }

    void Deform(float amount)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            var point = restVertices[i];
            float t = Mathf.Clamp((point.x + point.y) * .7071068f / halfLength, -1f, 1f);
            float bend = (1f - t * t) * amount * bendDistance;
            // Pull the string toward the archer, opposite the bow's outer curve.
            vertices[i] = point + new Vector3(.7071068f, -.7071068f, 0f) * bend;
        }
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    public void Release()
    {
        if(rigidDraw!=null){rigidDraw.Release();return;}
        if (activeMotion) recoilTime = .18f;
    }

    public void ResetDraw()
    {
        if(rigidDraw!=null)rigidDraw.ResetDraw();
        recoilTime = 0f;
        if (activeMotion && mesh != null) Deform(0f);
    }

    void Update()
    {
        if (!activeMotion || mesh == null || recoilTime <= 0f) return;
        recoilTime = Mathf.Max(0f, recoilTime - Time.deltaTime);
        float progress = 1f - recoilTime / .18f;
        Deform(lastDraw * Mathf.Cos(progress * Mathf.PI * 2f) * (1f - progress));
    }

    void OnDisable() { ResetDraw(); }
    void OnDestroy() { ClearVisual(); }
    void ClearVisual()
    {
        if(rigidDraw!=null){rigidDraw.Clear();rigidDraw=null;}
        activeMotion = false;
        if (source != null) source.enabled = true;
        if (visual != null) { visual.SetActive(false); Destroy(visual); }
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        visual = null; mesh = null; material = null;
    }
}
