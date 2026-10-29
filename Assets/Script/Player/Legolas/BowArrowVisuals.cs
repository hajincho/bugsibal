using UnityEngine;

public sealed class BowArrowVisuals : MonoBehaviour
{
    TrailRenderer trail;
    public float TrailSeconds { get; private set; }
    public TrailRenderer Trail => trail;

    public void Initialize(BowChargeSettings profile, float chargedSeconds)
    {
        if (profile == null || !profile.useGoldenEffects) return;
        TrailSeconds = profile.TrailDurationAt(chargedSeconds);
        var sprites = GetComponentsInChildren<SpriteRenderer>();
        SpriteRenderer first = null;
        foreach (var sprite in sprites)
        {
            if (first == null) first = sprite;
            sprite.color = Color.white;
            if (profile.goldSpriteMaterial != null) sprite.sharedMaterial = profile.goldSpriteMaterial;
            if (profile.goldGlowMaterial == null || sprite.sprite == null) continue;
            var glow = new GameObject("GoldenArrowGlow", typeof(SpriteRenderer));
            glow.transform.SetParent(sprite.transform, false);
            glow.transform.localScale = new Vector3(1.16f, 1.16f, 1f);
            var renderer = glow.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite.sprite;
            renderer.flipX = sprite.flipX;
            renderer.flipY = sprite.flipY;
            renderer.sharedMaterial = profile.goldGlowMaterial;
            renderer.color = new Color(1f, 1f, 1f, .45f);
            renderer.sortingLayerID = sprite.sortingLayerID;
            renderer.sortingOrder = sprite.sortingOrder;
        }
        if (TrailSeconds <= 0f || profile.goldGlowMaterial == null) return;
        // An independent root lets the existing trail finish after the arrow hits a wall or expires.
        var root = new GameObject("GoldenArrowAfterimage", typeof(TrailRenderer));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        root.layer = gameObject.layer;
        root.transform.position = transform.position;
        trail = root.GetComponent<TrailRenderer>();
        trail.sharedMaterial = profile.goldGlowMaterial;
        trail.time = TrailSeconds;
        trail.minVertexDistance = .045f;
        trail.startWidth = .10f;
        trail.endWidth = .008f;
        trail.startColor = new Color(1f, .8f, .13f, .85f);
        trail.endColor = new Color(1f, .48f, .025f, 0f);
        trail.numCornerVertices = 2;
        trail.numCapVertices = 2;
        trail.sortingLayerID = first != null ? first.sortingLayerID : 0;
        // Keep the tracer above stage backgrounds, just like the arrow itself.
        trail.sortingOrder = first != null ? first.sortingOrder : 0;
        trail.Clear();
        trail.emitting = true;
    }

    void LateUpdate()
    {
        if (trail != null) trail.transform.position = transform.position;
    }

    void FinishTrail()
    {
        if (trail == null) return;
        trail.transform.position = transform.position;
        trail.emitting = false;
        Destroy(trail.gameObject, TrailSeconds + .05f);
        trail = null;
    }
    void OnDisable() { FinishTrail(); }
    void OnDestroy() { FinishTrail(); }
}
