using System.Collections.Generic;
using UnityEngine;

// Procedural rings and light shards: this effect has no colliders or damage scripts.
public sealed class ArtemisiaBurst : MonoBehaviour
{
    readonly List<LineRenderer> rings = new List<LineRenderer>();
    readonly List<LineRenderer> rays = new List<LineRenderer>();
    float startedAt, radius;
    bool pink;
    bool customTint;
    Color tint;
    float widthScale = 1f;
    const float Duration = .45f;

    public void Initialize(Material material, float effectRadius, bool pinkEffect = false)
    {
        pink = pinkEffect;
        startedAt = Time.time;
        radius = effectRadius;
        for (int i = 0; i < 2; i++) rings.Add(CreateLine(material, "SkyShockRing", 49, true));
        for (int i = 0; i < 14; i++) rays.Add(CreateLine(material, "SkyLightShard", 2, false));
        Update();
    }

    public void InitializeColored(Material material, float effectRadius, Color color, float sizeScale = 1f)
    {
        customTint = true; tint = color; widthScale = sizeScale;
        Initialize(material, effectRadius);
    }

    LineRenderer CreateLine(Material material, string label, int count, bool loop)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var line = child.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = false;
        line.positionCount = count;
        line.loop = loop;
        line.numCapVertices = 2;
        line.sortingOrder = 8;
        return line;
    }

    void Update()
    {
        float t = Mathf.Clamp01((Time.time - startedAt) / Duration);
        float fade = (1f - t) * (1f - t);
        for (int j = 0; j < rings.Count; j++)
        {
            var line = rings[j];
            float size = radius * Mathf.Lerp(.08f, j == 0 ? 1f : .7f, Mathf.Sqrt(t));
            for (int i = 0; i < line.positionCount; i++)
            {
                float a = i * Mathf.PI * 2f / (line.positionCount - 1);
                line.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * size);
            }
            line.widthMultiplier = Mathf.Lerp(.09f, .015f, t) * widthScale;
            line.startColor = line.endColor = customTint ? new Color(tint.r, tint.g, tint.b, fade * (j == 0 ? .9f : .65f)) : pink ? new Color(1f, .7f, .84f, fade * (j == 0 ? .9f : .65f)) : new Color(.55f, .9f, 1f, fade * (j == 0 ? .9f : .65f));
        }
        for (int i = 0; i < rays.Count; i++)
        {
            float a = i * 2.39996323f;
            var direction = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            float end = radius * Mathf.Lerp(.12f, .75f + (i % 3) * .12f, Mathf.Sqrt(t));
            rays[i].SetPosition(0, direction * end * Mathf.Lerp(.1f, .85f, t));
            rays[i].SetPosition(1, direction * end);
            rays[i].startWidth = .065f * (1f - t) * widthScale;
            rays[i].endWidth = .008f * widthScale;
            rays[i].startColor = customTint ? new Color(1,1,1,fade) : pink ? new Color(1f, .85f, .93f, fade) : new Color(.8f, .97f, 1f, fade);
            rays[i].endColor = customTint ? new Color(tint.r,tint.g,tint.b,0) : pink ? new Color(1f, .55f, .75f, 0f) : new Color(.2f, .75f, 1f, 0f);
        }
        if (t >= 1f) Destroy(gameObject);
    }
}
