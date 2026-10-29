using UnityEngine;

[DefaultExecutionOrder(10000)]
public sealed class EnemyFrostStatus : MonoBehaviour
{
    EnemyData enemy;
    float slowUntil, frozenUntil, slowFactor = 1f;
    Animator[] animators;
    float[] animatorSpeeds;
    SpriteRenderer[] renderers;
    Color[] originalColors;
    Rigidbody2D[] bodies;
    RigidbodyConstraints2D[] constraints;
    bool frozenApplied;
    Vector3 frozenPosition;
    public bool IsFrozen => enemy != null && enemy.enemy_current_HP > 0f && Time.time < frozenUntil;
    public float FrozenRemaining => Mathf.Max(0f, frozenUntil - Time.time);
    public float MovementMultiplier => IsFrozen ? 0f : Time.time < slowUntil ? slowFactor : 1f;

    void Awake() { enemy = GetComponent<EnemyData>(); }

    public static bool IsImmune(EnemyData data)
    {
        return data == null || data.GetComponent<SwordMan>() != null ||
            data.name.StartsWith("FinalBoss", System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFrozenOn(Component component)
    {
        var status = component.GetComponentInParent<EnemyFrostStatus>();
        return status != null && status.IsFrozen;
    }

    public static float SpeedMultiplier(Component component)
    {
        var status = component.GetComponentInParent<EnemyFrostStatus>();
        return status != null ? status.MovementMultiplier : 1f;
    }

    public void Slow(float multiplier, float duration)
    {
        if (IsImmune(enemy) || enemy.enemy_current_HP <= 0f) return;
        slowFactor = Mathf.Clamp01(multiplier); slowUntil = Time.time + Mathf.Max(0f, duration);
    }

    public void Freeze(float duration)
    {
        if (IsImmune(enemy) || enemy.enemy_current_HP <= 0f) return;
        frozenUntil = Time.time + Mathf.Max(0f, duration);
        if (!frozenApplied)
        {
            frozenApplied = true; frozenPosition = transform.position;
            animators = GetComponentsInChildren<Animator>(true); animatorSpeeds = new float[animators.Length];
            renderers = GetComponentsInChildren<SpriteRenderer>(true); originalColors = new Color[renderers.Length];
            bodies = GetComponentsInChildren<Rigidbody2D>(true); constraints = new RigidbodyConstraints2D[bodies.Length];
            for (int i = 0; i < animators.Length; i++) animatorSpeeds[i] = animators[i].speed;
            for (int i = 0; i < renderers.Length; i++) originalColors[i] = renderers[i].color;
            for (int i = 0; i < bodies.Length; i++) constraints[i] = bodies[i].constraints;
        }
        HoldFrozen();
    }

    void HoldFrozen()
    {
        transform.position = frozenPosition;
        for (int i = 0; i < animators.Length; i++) if (animators[i] != null) animators[i].speed = 0f;
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null)
            renderers[i].color = new Color(.25f, .6f, 1f, Mathf.Max(.75f, originalColors[i].a));
        for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null && bodies[i].simulated)
        { bodies[i].velocity = Vector2.zero; bodies[i].angularVelocity = 0f; bodies[i].constraints = RigidbodyConstraints2D.FreezeAll; }
    }

    void FixedUpdate() { if (IsFrozen) HoldFrozen(); }
    void LateUpdate()
    {
        if (IsFrozen) HoldFrozen();
        else if (frozenApplied) RestoreFreeze();
    }

    void RestoreFreeze()
    {
        if (!frozenApplied) return;
        frozenApplied = false;
        for (int i = 0; i < animators.Length; i++) if (animators[i] != null) animators[i].speed = animatorSpeeds[i];
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] != null) renderers[i].color = originalColors[i];
        for (int i = 0; i < bodies.Length; i++) if (bodies[i] != null) bodies[i].constraints = constraints[i];
    }

    public void MoveFrozenAnchor(Vector2 position)
    {
        if (!IsFrozen) return;
        frozenPosition.x = position.x;
        frozenPosition.y = position.y;
        HoldFrozen();
    }

    public void CancelStatus()
    {
        slowUntil = frozenUntil = 0f; slowFactor = 1f; RestoreFreeze();
    }
    void OnDisable() { CancelStatus(); }
}
