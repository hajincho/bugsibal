using System.Collections.Generic;
using UnityEngine;

public sealed class BowQueenImpact : MonoBehaviour
{
    float dotDps, dotSeconds, radius, duration, speed, forwardOffset;
    Sprite gatheringWaveSprite;
    Transform source;
    Vector2 launchDirection;
    bool fullChargePull, pulled;
    public bool HasPulled => pulled;

    public void Initialize(BowChargeSettings profile, float chargeSeconds, Transform shooter = null)
    {
        // Snapshot at launch so switching weapons does not change arrows already in flight.
        gatheringWaveSprite=profile.queenGatherWaveSprite;
        dotDps = Mathf.Max(0f, profile.dotDamagePerSecond);
        dotSeconds = Mathf.Max(0f, profile.dotDuration);
        fullChargePull = profile.pullOnFullCharge &&
            chargeSeconds >= Mathf.Max(0.01f, profile.maxChargeSeconds);
        radius = Mathf.Max(0f, profile.pullRadius);
        duration = Mathf.Max(0.01f, profile.pullDuration);
        speed = Mathf.Max(0f, profile.pullSpeed);
        forwardOffset = Mathf.Max(0f, profile.pullForwardOffset);
        source = shooter;
        var body = GetComponent<Rigidbody2D>();
        launchDirection = body != null ? body.velocity.normalized : Vector2.right;
        if (launchDirection.sqrMagnitude < 0.0001f) launchDirection = Vector2.right;
    }

    public void OnImpact(EnemyData enemy, Vector2 position)
    {
        if (enemy != null && enemy.enemy_current_HP > 0f && dotDps > 0f && dotSeconds > 0f)
        {
            var dot = enemy.GetComponent<BowDamageOverTime>();
            if (dot == null) dot = enemy.gameObject.AddComponent<BowDamageOverTime>();
            dot.Apply(dotDps, dotSeconds);
        }

        // Both an enemy hit and a wall hit can be the full-charge impact point.
        if (!fullChargePull || pulled || radius <= 0f || speed <= 0f) return;
        pulled = true;
        QueenGatherPulse.Create(position,radius,duration,gatheringWaveSprite);
        Vector2 away = source != null ? position - (Vector2)source.position : launchDirection;
        if (away.sqrMagnitude < 0.0001f) away = launchDirection;
        Vector2 gatheringPoint = position + away.normalized * forwardOffset;
        var targets = new HashSet<EnemyData>();
        var pulls = new List<EnemyBowPull>();
        foreach (var collider in Physics2D.OverlapCircleAll(position, radius))
        {
            var target = collider.GetComponentInParent<EnemyData>();
            if (target == null || target.enemy_current_HP <= 0f || !target.CompareTag("Enemy") ||
                !targets.Add(target)) continue;
            var pull = target.GetComponent<EnemyBowPull>();
            if (pull == null) pull = target.gameObject.AddComponent<EnemyBowPull>();
            pull.PullTo(gatheringPoint, speed, duration);
            pulls.Add(pull);
        }
        for (int i = 0; i < pulls.Count; i++)
            for (int j = i + 1; j < pulls.Count; j++)
                pulls[i].IgnoreCollisionsWith(pulls[j]);
    }
}
