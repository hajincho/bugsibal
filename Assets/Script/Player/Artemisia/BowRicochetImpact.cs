using System.Collections.Generic;
using UnityEngine;

public sealed class BowRicochetImpact : MonoBehaviour
{
    int bouncesLeft;
    Collider2D lastWall;
    float lastBounceAt = -1f;
    GameObject explosionPrefab;
    Material explosionMaterial;
    float radius, visualScale;
    public int BouncesLeft => bouncesLeft;
    public int ExplosionCount { get; private set; }

    public void Initialize(BowChargeSettings profile, float seconds, BowExplosiveArrowSkill q)
    {
        explosionMaterial = profile.goldSpriteMaterial;
        bouncesLeft = seconds >= Mathf.Max(.01f, profile.maxChargeSeconds) ? Mathf.Max(0, profile.fullChargeRicochets) : 0;
        if (q != null) { explosionPrefab = q.explosionPrefab; radius = q.explosionRadius * .5f; visualScale = q.explosionScale * .5f; }
    }

    public bool TryBounce(Collider2D wall, Vector2 normal, Vector2 position, Rigidbody2D body)
    {
        // Sweep and trigger callbacks can report the same wall in one physics step.
        if (wall == lastWall && Time.fixedTime <= lastBounceAt + Time.fixedDeltaTime * 1.5f) return true;
        if (bouncesLeft <= 0) return false;
        Vector2 incoming = body.velocity;
        if (normal.sqrMagnitude < .0001f) normal = -incoming.normalized;
        normal.Normalize();
        if (Vector2.Dot(incoming, normal) > 0f) normal = -normal;
        Vector2 reflected = Vector2.Reflect(incoming, normal);
        // Even an exactly tangent contact must bounce instead of silently passing through.
        if (Vector2.Dot(reflected.normalized, incoming.normalized) > .9999f) reflected = -incoming;
        bouncesLeft--;
        lastWall = wall; lastBounceAt = Time.fixedTime;
        body.position = position + normal * .04f;
        body.velocity = reflected;
        body.rotation = Mathf.Atan2(body.velocity.y, body.velocity.x) * Mathf.Rad2Deg + 180f;
        return true;
    }

    public void Explode(EnemyData primary, Vector3 position, float arrowDamage)
    {
        ExplosionCount++;
        if (explosionPrefab != null)
        {
            var effect = Instantiate(explosionPrefab, position, Quaternion.identity);
            effect.name = "ArtemisiaImpactExplosion";
            effect.transform.localScale = explosionPrefab.transform.localScale * visualScale;
            foreach (var c in effect.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            foreach (var b in effect.GetComponentsInChildren<Rigidbody2D>(true)) b.simulated = false;
            foreach (var script in effect.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (var r in effect.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (explosionMaterial != null) r.sharedMaterial = explosionMaterial;
                r.color = new Color(1f, 1f, 1f, r.color.a * .7f);
            }
            Destroy(effect, .4f);
        }
        var targets = new HashSet<EnemyData>();
        if (primary != null) targets.Add(primary);
        foreach (var c in Physics2D.OverlapCircleAll(position, radius))
        {
            var enemy = c.GetComponentInParent<EnemyData>();
            if (enemy != null && enemy.CompareTag("Enemy")) targets.Add(enemy);
        }
        foreach (var enemy in targets)
            if (enemy != null && enemy.enemy_current_HP > 0f) { enemy.TakeDamage(Mathf.Max(0f, arrowDamage * .5f), CombatDamageKind.Normal); enemy.Hitted(); }
    }
}
