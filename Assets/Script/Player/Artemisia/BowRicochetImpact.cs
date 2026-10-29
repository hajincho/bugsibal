using System.Collections.Generic;
using UnityEngine;

public sealed class BowRicochetImpact : MonoBehaviour
{
    bool canBounce, detonated;
    bool pinkEffects, explodeOnEnemyHit;
    float bounceDamageBonus = .2f;
    Collider2D lastWall;
    float lastBounceAt = -1f;
    Material explosionMaterial;
    float radius, initialDamage;
    Arrow arrow;
    public bool ExplodesOnContact { get; private set; }
    public int BounceCount { get; private set; }
    public int ExplosionCount { get; private set; }
    public float DamageMultiplier => 1f + BounceCount * bounceDamageBonus;

    public void Initialize(BowChargeSettings profile, float seconds, BowExplosiveArrowSkill q)
    {
        arrow = GetComponent<Arrow>();
        initialDamage = arrow != null ? arrow.Damage : 0f;
        explosionMaterial = profile.goldGlowMaterial;
        ExplodesOnContact = profile.explodeOnAnyContact;
        canBounce = !ExplodesOnContact && (profile.ricochetWithoutFullCharge || seconds >= Mathf.Max(.01f, profile.maxChargeSeconds));
        bounceDamageBonus = profile.ricochetDamageBonus;
        pinkEffects = profile.usePinkEffects;
        explodeOnEnemyHit = profile.explodeOnRicochetEnemyHit;
        radius = q != null ? q.explosionRadius : 3.25f;
    }

    public bool TryBounce(Collider2D wall, Vector2 normal, Vector2 position, Rigidbody2D body)
    {
        if (!canBounce || detonated) return false;
        // Ignore duplicate sweep/trigger contacts without consuming another bounce.
        if (wall == lastWall && Time.fixedTime <= lastBounceAt + Time.fixedDeltaTime * 1.5f) return true;
        Vector2 incoming = body.velocity;
        if (normal.sqrMagnitude < .0001f) normal = -incoming.normalized;
        normal.Normalize();
        if (Vector2.Dot(incoming, normal) > 0f) normal = -normal;
        Vector2 reflected = Vector2.Reflect(incoming, normal);
        if (Vector2.Dot(reflected.normalized, incoming.normalized) > .9999f) reflected = -incoming;
        BounceCount++;
        if (arrow != null) arrow.ApplyRicochetDamage(initialDamage * DamageMultiplier);
        lastWall = wall; lastBounceAt = Time.fixedTime;
        body.position = position + normal * .04f;
        body.velocity = reflected;
        body.rotation = Mathf.Atan2(body.velocity.y, body.velocity.x) * Mathf.Rad2Deg + 180f;
        return true;
    }

    public void OnEnemyHit(EnemyData enemy, Vector3 position, float damage)
    {
        if (explodeOnEnemyHit && BounceCount > 0 && !detonated) Explode(enemy, position, damage);
    }

    public void Detonate() { DetonateOnContact(null, transform.position); }

    public void DetonateOnContact(EnemyData primary, Vector3 position)
    {
        if (detonated) return;
        detonated = true;
        CancelInvoke();
        Explode(primary, position, arrow != null ? arrow.Damage : initialDamage);
        Destroy(gameObject);
    }

    void Explode(EnemyData primary, Vector3 position, float arrowDamage)
    {
        ExplosionCount++;
        var effect = new GameObject(pinkEffects ? "JudgementPinkBurst" : "ArtemisiaSkyBurst");
        effect.transform.position = position;
        effect.AddComponent<ArtemisiaBurst>().Initialize(explosionMaterial, radius, pinkEffects);
        var targets = new HashSet<EnemyData>();
        if (primary != null) targets.Add(primary);
        foreach (var c in Physics2D.OverlapCircleAll(position, radius))
        {
            var enemy = c.GetComponentInParent<EnemyData>();
            if (enemy != null && enemy.CompareTag("Enemy")) targets.Add(enemy);
        }
        foreach (var enemy in targets)
            if (enemy != null && enemy.enemy_current_HP > 0f)
            { enemy.TakeDamage(Mathf.Max(0f, arrowDamage * .5f), CombatDamageKind.Normal); enemy.Hitted(); }
    }
}
