using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(500)]
public sealed class BowIcicleOrbit : MonoBehaviour
{
    sealed class Shard
    {
        public Transform transform;
        public SpriteRenderer sprite;
        public TrailRenderer trail;
        public float angle, radius, speed, expiresAt;
        public float axisRatio, orbitTilt, wobblePhase, precession;
        public bool expired;
        public Vector2 previous;
        public HashSet<EnemyData> contacts = new HashSet<EnemyData>();
        public HashSet<EnemyData> current = new HashSet<EnemyData>();
    }
    readonly List<Shard> shards = new List<Shard>();
    readonly Dictionary<EnemyData, int> stacks = new Dictionary<EnemyData, int>();
    readonly RaycastHit2D[] hits = new RaycastHit2D[128];
    BowIcicleSkill skill;
    Player player;
    float cleanupAt;
    float startedAt;
    public int LiveCount { get { int count = 0; foreach (var s in shards) if (!s.expired) count++; return count; } }
    public Vector3 Center => transform.position;

    public void Initialize(BowIcicleSkill config, Player owner)
    {
        skill = config; player = owner;
        transform.position = owner.transform.position + skill.centerOffset;
        float now = Time.time;
        startedAt = now;
        cleanupAt = now + skill.maxLifetime + 1.1f;
        var playerSprite = owner.GetComponentInChildren<SpriteRenderer>();
        // One random orbit in each radial band keeps the whole radius populated.
        var radii = new float[Mathf.Max(0, skill.icicleCount)];
        for (int i = 0; i < radii.Length; i++)
            radii[i] = Mathf.Lerp(skill.minRadius, skill.maxRadius, (i + Random.value) / radii.Length);
        for (int i = radii.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            float swap = radii[i]; radii[i] = radii[j]; radii[j] = swap;
        }
        for (int i = 0; i < skill.icicleCount; i++)
        {
            var go = new GameObject("Icicle_" + (i + 1), typeof(SpriteRenderer), typeof(TrailRenderer));
            go.transform.SetParent(transform, false);
            var sprite = go.GetComponent<SpriteRenderer>();
            sprite.sprite = skill.icicleSprite;
            sprite.sortingLayerID = playerSprite != null ? playerSprite.sortingLayerID : 0;
            sprite.sortingOrder = 15;
            float scale = skill.icicleHeight / Mathf.Max(.01f, skill.icicleSprite.bounds.size.y);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var trail = go.GetComponent<TrailRenderer>();
            trail.sharedMaterial = skill.trailMaterial;
            trail.time = 1f; trail.minVertexDistance = .035f;
            trail.startWidth = .045f; trail.endWidth = .003f;
            trail.startColor = new Color(.35f, .85f, 1f, .65f);
            trail.endColor = new Color(.12f, .4f, 1f, 0f);
            trail.sortingLayerID = sprite.sortingLayerID; trail.sortingOrder = sprite.sortingOrder - 1;
            trail.numCornerVertices = 2; trail.numCapVertices = 2; trail.emitting = false;
            var shard = new Shard { transform = go.transform, sprite = sprite, trail = trail,
                angle = i * Mathf.PI * 2f / Mathf.Max(1, skill.icicleCount),
                radius = radii[i],
                axisRatio = Random.Range(.82f, .94f),
                orbitTilt = i * 2.39996323f + Random.Range(-.15f, .15f),
                wobblePhase = Random.Range(0f, Mathf.PI * 2f),
                precession = Random.Range(-.22f, .22f),
                speed = skill.RotationSpeedAtRadius(radii[i]) * (Random.value < .5f ? -1f : 1f),
                expiresAt = now + Random.Range(skill.minLifetime, skill.maxLifetime) };
            Position(shard); shard.previous = go.transform.position;
            trail.Clear(); trail.emitting = true;
            shards.Add(shard);
        }
    }

    void Position(Shard s)
    {
        float elapsed = Time.time - startedAt;
        Vector2 point = OrbitPoint(s, s.angle, elapsed);
        s.transform.localPosition = new Vector3(point.x, point.y, 0f);
        Vector2 tangent = OrbitPoint(s, s.angle + Mathf.Sign(s.speed) * .01f, elapsed) - point;
        // Keep the downward-pointing shard aligned to its irregular path.
        s.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + 90f);
    }

    Vector2 OrbitPoint(Shard s, float angle, float elapsed)
    {
        // Mild flattening and smooth radial waves prevent rigid circles or sudden jumps.
        float wobble = 1f + .04f * Mathf.Sin(angle * 2f + s.wobblePhase)
            + .025f * Mathf.Sin(elapsed * 1.7f + s.wobblePhase);
        Vector2 ellipse = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * s.axisRatio) * s.radius * wobble;
        float tilt = s.orbitTilt + elapsed * s.precession;
        Vector2 point = new Vector2(ellipse.x * Mathf.Cos(tilt) - ellipse.y * Mathf.Sin(tilt),
            ellipse.x * Mathf.Sin(tilt) + ellipse.y * Mathf.Cos(tilt));
        return Vector2.ClampMagnitude(point, skill.maxRadius);
    }

    void FixedUpdate()
    {
        if (player == null || skill == null || GameManager.player_current_HP <= 0f) { Destroy(gameObject); return; }
        if (StageRewardController.InputBlocked) return;
        transform.position = player.transform.position + skill.centerOffset;
        foreach (var s in shards)
        {
            if (s.expired) continue;
            if (Time.time >= s.expiresAt)
            {
                s.expired = true; s.sprite.enabled = false; s.trail.emitting = false; s.contacts.Clear();
                // Let the one-second trail finish at the actual last world position.
                s.transform.SetParent(null, true); Destroy(s.transform.gameObject, 1.05f);
                continue;
            }
            s.angle += s.speed * Mathf.Deg2Rad * Time.fixedDeltaTime;
            Position(s);
            Vector2 current = s.transform.position;
            Vector2 travel = current - s.previous;
            s.current.Clear();
            int count = Physics2D.CircleCastNonAlloc(s.previous, skill.hitRadius,
                travel.sqrMagnitude > .000001f ? travel.normalized : Vector2.right, hits, travel.magnitude);
            for (int i = 0; i < count; i++)
            {
                var enemy = hits[i].collider != null ? hits[i].collider.GetComponentInParent<EnemyData>() : null;
                if (enemy == null || enemy.enemy_current_HP <= 0f || !enemy.CompareTag("Enemy") || !s.current.Add(enemy)) continue;
                if (!s.contacts.Contains(enemy)) RegisterHit(enemy);
            }
            var previousContacts = s.contacts; s.contacts = s.current; s.current = previousContacts;
            s.previous = current;
        }
        if (Time.time >= cleanupAt) Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (player != null) transform.position = player.transform.position + skill.centerOffset;
    }

    public int StackCount(EnemyData enemy) { int count; return enemy != null && stacks.TryGetValue(enemy, out count) ? count : 0; }

    public bool RegisterHit(EnemyData enemy)
    {
        if (enemy == null || enemy.enemy_current_HP <= 0f || StageRewardController.InputBlocked) return false;
        enemy.TakeDamage(skill.damage, CombatDamageKind.Skill);
        if (enemy.enemy_current_HP <= 0f || EnemyFrostStatus.IsImmune(enemy)) return true;
        int count = StackCount(enemy) + 1;
        var frost = enemy.GetComponent<EnemyFrostStatus>();
        if (frost == null) frost = enemy.gameObject.AddComponent<EnemyFrostStatus>();
        frost.Slow(skill.slowMultiplier, skill.slowSeconds);
        if (count >= Mathf.Max(1, skill.hitsToFreeze)) { frost.Freeze(skill.freezeSeconds); count = 0; }
        stacks[enemy] = count;
        return true;
    }
}
