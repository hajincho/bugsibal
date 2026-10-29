using UnityEngine;

public sealed class BowIcicleSkill : MonoBehaviour
{
    public Sprite icicleSprite;
    public Material trailMaterial;
    [Min(0f)] public float trailSeconds = .8f;
    public CoolDown cooldown;
    [Min(0.1f)] public float cooldownSeconds = 16f;
    public int icicleCount = 20;
    public float minLifetime = 4f;
    public float maxLifetime = 4.5f;
    public float minRadius = 1f;
    public float maxRadius = 5.72f;
    public float angularSpeed = 351f;
    [Range(0f, 1f)] public float innerSpeedBonus = .25f;
    public float icicleHeight = 1.08f;
    public float hitRadius = .264f;
    [Tooltip("Minimum damage per icicle hit.")]
    public float damage = 1f;
    [Range(0f, 1f)] public float maxHealthDamageFraction = .002f;
    [Header("타격 누적 회복")]
    [Min(1)] public int hitsToHeal = 10;
    [Min(0f)] public float healAmount = 2f;
    int healingHitCount;
    public int HealingHitCount => healingHitCount;

    [Min(1)] public int hitsToFreeze = 10;
    public float slowMultiplier = 0.6f;
    public float slowSeconds = 2f;
    public float freezeSeconds = 4f;
    public Vector3 centerOffset = new Vector3(0f, 1.3f, 0f);
    public BowIcicleOrbit ActiveOrbit { get; private set; }
    float readyAt;
    public float RemainingCooldown => Mathf.Max(0f, readyAt - Time.time);

    public float DamageForMaxHealth(float maxHealth)
    {
        return Mathf.Max(0f, Mathf.Max(damage, Mathf.Max(0f, maxHealth) * maxHealthDamageFraction) * GameManager.BowSkillDamageMultiplier);
    }

    public void ResetHealingHits() { healingHitCount = 0; }

    public void RegisterHealingHit()
    {
        if (GameManager.player_current_HP <= 0f) { ResetHealingHits(); return; }
        healingHitCount++;
        if (healingHitCount >= Mathf.Max(1, hitsToHeal))
        {
            healingHitCount = 0;
            GameManager.HealPlayer(healAmount);
        }
    }

    public float RotationSpeedAtRadius(float radius)
    {
        float inward = 1f - Mathf.InverseLerp(minRadius, maxRadius, radius);
        return angularSpeed * (1f + innerSpeedBonus * inward);
    }

    void Update()
    {
        if (GameManager.player_current_HP <= 0f) ResetHealingHits();
        if (Input.GetKeyDown(KeyCode.E)) Cast();
    }

    public bool Cast()
    {
        if (RemainingCooldown > 0f || icicleSprite == null || !WeaponSkillLoadout.CanUse(this)) return false;
        var player = GetComponent<Player>();
        if (player == null) return false;
        readyAt = Time.time + cooldownSeconds;
        if (cooldown != null) { cooldown.skillcooltime = cooldownSeconds; cooldown.UseSkill(); }
        var root = new GameObject("BowE_IcicleOrbit");
        DontDestroyOnLoad(root);
        ActiveOrbit = root.AddComponent<BowIcicleOrbit>();
        ActiveOrbit.Initialize(this, player);
        return true;
    }
}
