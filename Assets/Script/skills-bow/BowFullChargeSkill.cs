using UnityEngine;

[DisallowMultipleComponent]
public sealed class BowFullChargeSkill : MonoBehaviour
{
    const float DurationSeconds = 10f;
    const float CooldownSeconds = 40f;
    const float MoveSpeedBonus = 0.08f;
    float expiresAt;
    float readyAt;
    Player owner;
    public TMPro.TMP_Text statusText;

    public bool IsActive => isActiveAndEnabled && GameManager.player_current_HP > 0f && Time.time < expiresAt;
    public float RemainingDuration => IsActive ? Mathf.Max(0f, expiresAt - Time.time) : 0f;
    public float RemainingCooldown => Mathf.Max(0f, readyAt - Time.time);
    public float MoveSpeedMultiplier => IsActive ? 1f + MoveSpeedBonus : 1f;

    void Awake() { owner = GetComponent<Player>(); }

    void Start()
    {
        var loadout = GetComponent<WeaponSkillLoadout>();
        if (loadout != null)
        {
            if (loadout.bowSkills == null) loadout.bowSkills = new WeaponSkillSlot[3];
            else if (loadout.bowSkills.Length < 3) System.Array.Resize(ref loadout.bowSkills, 3);
            if (loadout.bowSkills[2] == null) loadout.bowSkills[2] = new WeaponSkillSlot();
            loadout.bowSkills[2].skill = this;
            loadout.Refresh();
        }
        UpdateStatus();
    }

    void Update()
    {
        if (GameManager.player_current_HP <= 0f) CancelBuff();
        if (Input.GetKeyDown(KeyCode.X)) Cast();
        UpdateStatus();
    }

    public bool Cast()
    {
        // This instant buff can also be activated while drawing the bow.
        if (!isActiveAndEnabled || owner == null || owner.weapon_mode != 0 || owner.ownedBow == null ||
            owner.on_dash || owner.skill_using || StageRewardController.InputBlocked || Time.timeScale <= 0f ||
            GameManager.player_current_HP <= 0f || RemainingCooldown > 0f) return false;

        expiresAt = Time.time + DurationSeconds;
        readyAt = Time.time + CooldownSeconds;
        UpdateStatus();
        return true;
    }

    void UpdateStatus()
    {
        if (statusText == null) return;
        if (IsActive)
        {
            statusText.text = RemainingDuration.ToString("0.0") + "s";
            statusText.color = new Color(1f, 0.85f, 0.25f);
        }
        else if (RemainingCooldown > 0f)
        {
            statusText.text = Mathf.CeilToInt(RemainingCooldown) + "s";
            statusText.color = new Color(0.75f, 0.75f, 0.75f);
        }
        else
        {
            statusText.text = "READY";
            statusText.color = Color.white;
        }
    }

    public void CancelBuff() { expiresAt = 0f; UpdateStatus(); }
    void OnDisable() { CancelBuff(); }
}
