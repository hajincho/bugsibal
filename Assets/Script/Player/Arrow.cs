using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public bool HasHit { get; private set; }
    public float Damage { get; private set; }
    public bool PiercesEnemies { get; private set; }
    readonly HashSet<EnemyData> hitEnemies = new HashSet<EnemyData>();
    Rigidbody2D rigid;
    Transform owner;
    bool launched;
    readonly RaycastHit2D[] hits = new RaycastHit2D[32];

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
        rigid.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnEnable()
    {
        HasHit = false;
        launched = false;
        hitEnemies.Clear();
        Destroy(gameObject, 10f); // Also clean up an unlaunched instance.
    }

    public void Fire(Vector3 target)
    {
        Fire(target, 5f, 30f, 3f, null);
    }

    public void Fire(Vector3 target, float damage, float speed, float lifetime, Transform source, bool pierceEnemies = false)
    {
        owner = source;
        PiercesEnemies = pierceEnemies;
        hitEnemies.Clear();
        if (PiercesEnemies)
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.isTrigger = true;
        Damage = Mathf.Max(0f, damage);
        Vector2 direction = target - transform.position;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        direction.Normalize();
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f);
        rigid.velocity = direction * Mathf.Max(0.1f, speed);
        launched = true;
        if (GetComponent<BowRicochetImpact>() != null) Invoke(nameof(DetonateRicochetArrow), Mathf.Max(.1f, lifetime));
        else Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    void DetonateRicochetArrow()
    {
        var special = GetComponent<BowRicochetImpact>();
        if (special != null) special.Detonate();
        else Destroy(gameObject);
    }

    public void ApplyRicochetDamage(float value)
    {
        Damage = Mathf.Max(0f, value);
        hitEnemies.Clear();
    }

    void FixedUpdate()
    {
        if (!launched || HasHit || StageRewardController.InputBlocked) return;
        // Sweep the arrow's collider so a fast arrow cannot skip thin/overlapping hitboxes.
        Vector2 travel = rigid.velocity * Time.fixedDeltaTime;
        if (travel.sqrMagnitude <= 0f) return;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = true;
        int count = rigid.Cast(travel.normalized, filter, hits, travel.magnitude);
        // Always resolve closer contacts first, so a wall stops hits behind it.
        for (int i = 1; i < count; i++)
        {
            var hit = hits[i];
            int j = i - 1;
            while (j >= 0 && hits[j].distance > hit.distance) { hits[j + 1] = hits[j]; j--; }
            hits[j + 1] = hit;
        }
        for (int i = 0; i < count; i++)
        {
            bool wall = hits[i].collider != null && hits[i].collider.GetComponentInParent<EnemyData>() == null;
            if (TryHit(hits[i].collider, hits[i].normal, hits[i].centroid) && (!launched || wall)) break;
        }
    }

    void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
    void OnCollisionEnter2D(Collision2D other) { TryHit(other.collider); }

    public bool TryHit(Collider2D other)
    {
        Vector2 normal = other != null ? rigid.position - other.ClosestPoint(rigid.position) : Vector2.zero;
        if (normal.sqrMagnitude < .0001f) normal = -rigid.velocity.normalized;
        return TryHit(other, normal.normalized, rigid.position);
    }

    bool TryHit(Collider2D other, Vector2 normal, Vector2 contactPosition)
    {
        if (HasHit || !launched || other == null || StageRewardController.InputBlocked) return false;
        if (other.transform.IsChildOf(transform) || (owner != null && other.transform.IsChildOf(owner)) ||
            other.GetComponentInParent<Player>() != null || other.GetComponentInParent<Arrow>() != null) return false;
        EnemyData enemy = other.GetComponentInParent<EnemyData>();
        // Ignore queued contacts with an enemy that died earlier in this physics step.
        if (enemy != null && enemy.enemy_current_HP <= 0f) return false;
        // Static Rigidbody2D walls (including FinalBoss SlopeWall) are solid surfaces.
        // Continue ignoring triggers and movable props/pickups.
        if (enemy == null && (other.isTrigger ||
            (other.attachedRigidbody != null && other.attachedRigidbody.bodyType != RigidbodyType2D.Static))) return false;

        if (enemy != null && !hitEnemies.Add(enemy)) return false;
        var special = GetComponent<BowRicochetImpact>();
        if (enemy == null && special != null && special.TryBounce(other, normal, contactPosition, rigid)) return true;
        bool continues = enemy != null && PiercesEnemies && (special == null || !special.ExplodesOnContact);
        // Mark terminal hits before callbacks; piercing shots retain their velocity and colliders.
        if (!continues)
        {
            HasHit = true;
            launched = false;
            rigid.velocity = Vector2.zero;
            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) hitbox.enabled = false;
            rigid.simulated = false;
        }
        if (enemy != null)
        {
            var explosive = GetComponent<BowExplosiveArrowImpact>();
            if (explosive != null && !explosive.HasExploded)
            {
                explosive.Explode(enemy, transform.position, Damage);
                foreach (var damaged in explosive.DamagedEnemies) hitEnemies.Add(damaged);
            }
            else
            {
                enemy.TakeDamage(Damage, CombatDamageKind.Normal);
                enemy.Hitted();
            }
        }
        var queenImpact = GetComponent<BowQueenImpact>();
        if (queenImpact != null) queenImpact.OnImpact(enemy, contactPosition);
        if (special != null)
        {
            if (special.ExplodesOnContact) special.DetonateOnContact(enemy, transform.position);
            else if (enemy != null) special.OnEnemyHit(enemy, transform.position, Damage);
        }
        if (!continues && special == null) Destroy(gameObject);
        return true;
    }
}
