using UnityEngine;
using System.Collections.Generic;

// Apply after normal pursuit movement, without disabling or resetting the enemy AI.
[DefaultExecutionOrder(11000)]
public sealed class EnemyBowPull : MonoBehaviour
{
    EnemyData enemy;
    Rigidbody2D body;
    Collider2D hitbox;
    EnemyFrostStatus frost;
    Vector2 destination;
    float speed, remaining;
    float knockbackUntil;
    Vector2 commandedPosition;
    bool hasCommandedPosition;
    Collider2D[] colliders;
    sealed class CollisionPair { public Collider2D first, second; public int users; }
    static readonly Dictionary<ulong, CollisionPair> collisionPairs = new Dictionary<ulong, CollisionPair>();
    readonly HashSet<ulong> ignoredPairs = new HashSet<ulong>();
    readonly RaycastHit2D[] hits = new RaycastHit2D[32];

    void Awake()
    {
        enemy = GetComponent<EnemyData>();
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<Collider2D>();
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    public void PullTo(Vector2 point, float pullSpeed, float seconds)
    {
        if (enemy == null || enemy.enemy_current_HP <= 0f || Time.time < knockbackUntil) return;
        destination = point;
        speed = Mathf.Max(0f, pullSpeed);
        remaining = Mathf.Max(0f, seconds);
        frost = GetComponent<EnemyFrostStatus>();
        hasCommandedPosition = false;
        enabled = remaining > 0f && speed > 0f;
    }

    public void KnockbackTo(Vector2 point, float seconds)
    {
        if(enemy==null || enemy.enemy_current_HP<=0f)return;
        float duration=Mathf.Max(.02f,seconds);
        float distance=Vector2.Distance(transform.position,point);
        knockbackUntil=0f;
        PullTo(point,distance/duration*1.15f,duration);
        knockbackUntil=Time.time+duration;
    }

    public void IgnoreCollisionsWith(EnemyBowPull other)
    {
        if (other == null || other == this) return;
        foreach (var first in colliders)
            foreach (var second in other.colliders)
            {
                if (first == null || second == null || first == second) continue;
                uint a = unchecked((uint)first.GetInstanceID()), b = unchecked((uint)second.GetInstanceID());
                ulong key = a < b ? ((ulong)a << 32) | b : ((ulong)b << 32) | a;
                CollisionPair pair;
                if (!collisionPairs.TryGetValue(key, out pair))
                {
                    if (Physics2D.GetIgnoreCollision(first, second)) continue;
                    pair = new CollisionPair { first = first, second = second };
                    collisionPairs.Add(key, pair);
                    Physics2D.IgnoreCollision(first, second, true);
                }
                if (ignoredPairs.Add(key)) pair.users++;
                if (other.ignoredPairs.Add(key)) pair.users++;
            }
    }

    void FixedUpdate()
    {
        if (enemy == null || enemy.enemy_current_HP <= 0f || remaining <= 0f)
        { enabled = false; return; }
        if (StageRewardController.InputBlocked || Time.timeScale <= 0f) return;
        float elapsed = Mathf.Min(Time.fixedDeltaTime, remaining);
        remaining = Mathf.Max(0f, remaining - elapsed);
        Vector2 position = body != null ? body.position : (Vector2)transform.position;
        Vector2 offset = destination - position;
        float distance = Mathf.Min(speed * elapsed, offset.magnitude);
        Vector2 direction = offset.normalized;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;
        int count = distance <= 0.0001f ? 0 : body != null ? body.Cast(direction, filter, hits, distance) :
            hitbox != null ? hitbox.Cast(direction, filter, hits, distance) : 0;
        for (int i = 0; i < count; i++)
        {
            var obstacle = hits[i].collider;
            if (obstacle == null || obstacle.GetComponentInParent<EnemyData>() != null ||
                obstacle.GetComponentInParent<Player>() != null) continue;
            if (obstacle.attachedRigidbody == null || obstacle.attachedRigidbody.bodyType == RigidbodyType2D.Static)
                distance = Mathf.Min(distance, Mathf.Max(0f, hits[i].distance - 0.02f));
        }
        Vector2 next = position + direction * distance;
        commandedPosition = next;
        hasCommandedPosition = true;
        if (frost != null && frost.IsFrozen) frost.MoveFrozenAnchor(next);
        else if (body != null && body.bodyType != RigidbodyType2D.Static) body.MovePosition(next);
        else transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    void LateUpdate()
    {
        if (!hasCommandedPosition || StageRewardController.InputBlocked || Time.timeScale <= 0f) return;
        if (frost != null && frost.IsFrozen) frost.MoveFrozenAnchor(commandedPosition);
        else if (body != null)
        {
            body.position = commandedPosition;
            body.velocity = Vector2.zero;
        }
        else transform.position = new Vector3(commandedPosition.x, commandedPosition.y, transform.position.z);
    }

    void OnDisable()
    {
        remaining = 0f;
        knockbackUntil = 0f;
        hasCommandedPosition = false;
        foreach (ulong key in ignoredPairs)
        {
            CollisionPair pair;
            if (!collisionPairs.TryGetValue(key, out pair)) continue;
            pair.users--;
            if (pair.users <= 0)
            {
                if (pair.first != null && pair.second != null)
                    Physics2D.IgnoreCollision(pair.first, pair.second, false);
                collisionPairs.Remove(key);
            }
        }
        ignoredPairs.Clear();
        if (body != null && body.simulated && body.bodyType != RigidbodyType2D.Static)
            body.velocity = Vector2.zero;
    }
}
