using UnityEngine;

public sealed class BowExplosiveArrowSkill : MonoBehaviour
{
    [Min(0.1f)] public float cooldownSeconds = 8f;
    public CoolDown cooldown;
    public UnityEngine.UI.Image armedOverlay;
    public GameObject explosionPrefab;
    [Min(0.01f)] public float explosionScale = 2.25f;
    [Min(0.01f)] public float explosionRadius = 3.25f;
    public bool IsArmed { get; private set; }
    public bool IsCoolingDown => cooldown != null && cooldown.IsCoolingDown;
    Player owner;

    void Awake() { owner = GetComponent<Player>(); }
    void Start() { UpdateIndicator(); }
    void Update()
    {
        if (GameManager.player_current_HP <= 0f && IsArmed) { IsArmed = false; UpdateIndicator(); }
        if (Input.GetKeyDown(KeyCode.Q)) Arm();
    }

    public bool Arm()
    {
        // Arming is allowed during a bow draw and rat stun; it does not cast an animation.
        if (owner == null || owner.weapon_mode != 0 || owner.on_dash || owner.skill_using ||
            StageRewardController.InputBlocked || Time.timeScale <= 0f || GameManager.player_current_HP <= 0f ||
            IsArmed || cooldown == null || IsCoolingDown) return false;
        IsArmed = true;
        UpdateIndicator();
        return true;
    }

    public bool OnArrowFired(Arrow arrow)
    {
        if (!IsArmed || arrow == null) return false;
        var impact = arrow.gameObject.AddComponent<BowExplosiveArrowImpact>();
        impact.explosionPrefab = explosionPrefab;
        impact.visualScale = explosionScale;
        impact.radius = explosionRadius;
        // Keep the equipped bow skill multiplier with this shot if equipment changes in flight.
        impact.skillDamageMultiplier = GameManager.BowSkillDamageMultiplier;
        foreach (var renderer in arrow.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.color = new Color(1f, 0.25f, 0.2f, renderer.color.a);
        IsArmed = false;
        cooldown.skillcooltime = cooldownSeconds;
        cooldown.UseSkill();
        UpdateIndicator();
        return true;
    }

    public void CancelArmed()
    {
        IsArmed = false;
        UpdateIndicator();
    }

    void UpdateIndicator()
    {
        if (armedOverlay != null) armedOverlay.gameObject.SetActive(IsArmed);
    }
}
