using UnityEngine;

public sealed class BowDamageOverTime : MonoBehaviour
{
    EnemyData enemy;
    float damagePerSecond, remaining, pendingSeconds;
    public float RemainingSeconds => remaining;

    void Awake() { enemy = GetComponent<EnemyData>(); }

    public void Apply(float dps, float duration)
    {
        if (enemy == null || enemy.enemy_current_HP <= 0f || dps <= 0f || duration <= 0f) return;
        if (remaining <= 0f) pendingSeconds = 0f;
        damagePerSecond = dps;
        remaining = duration;
        enabled = true;
    }

    void Update()
    {
        if (enemy == null || enemy.enemy_current_HP <= 0f) { enabled = false; return; }
        if (StageRewardController.InputBlocked || Time.timeScale <= 0f) return;
        float elapsed = Mathf.Min(Time.deltaTime, remaining);
        remaining = Mathf.Max(0f, remaining - elapsed);
        pendingSeconds += elapsed;
        while (pendingSeconds >= 1f && enemy.enemy_current_HP > 0f)
        {
            pendingSeconds -= 1f;
            enemy.TakeDamage(damagePerSecond, CombatDamageKind.Normal);
        }
        if (remaining <= 0f)
        {
            if (pendingSeconds > 0.0001f && enemy.enemy_current_HP > 0f)
                enemy.TakeDamage(damagePerSecond * pendingSeconds, CombatDamageKind.Normal);
            enabled = false;
        }
    }

    void OnDisable() { remaining = pendingSeconds = 0f; }
}
