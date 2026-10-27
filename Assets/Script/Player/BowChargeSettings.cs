using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Bow Charge Settings", fileName = "BowSettings")]
public sealed class BowChargeSettings : ScriptableObject
{
    public Sprite idleSprite;
    public Sprite[] drawSprites;
    public GameObject arrowPrefab;

    [Min(0.01f)] public float maxChargeSeconds = 1.5f;
    [Min(0f)] public float minimumDamage = 5f;
    [Min(0.01f)] public float damageStepSeconds = 0.1f;
    [Tooltip("Raw bow power per step; this game's bow multiplier converts 2 power to 1 damage.")]
    [Min(0f)] public float powerPerStep = 2f;
    [Min(0f)] public float bowDamageMultiplier = 0.5f;
    [Min(0.1f)] public float arrowSpeed = 30f;
    [Min(0.1f)] public float arrowLifetime = 3f;

    public float ChargeFraction(float seconds)
    {
        return Mathf.Clamp01(seconds / Mathf.Max(0.01f, maxChargeSeconds));
    }

    public float DamageAt(float seconds)
    {
        float held = Mathf.Clamp(seconds, 0f, Mathf.Max(0.01f, maxChargeSeconds));
        int steps = Mathf.FloorToInt((held + 0.00001f) / Mathf.Max(0.01f, damageStepSeconds));
        return minimumDamage + steps * powerPerStep * bowDamageMultiplier;
    }
}
