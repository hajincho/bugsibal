using System;
using UnityEngine;

[Serializable]
public sealed class WeaponSkillSlot
{
    public MonoBehaviour skill;
    public GameObject hud;
}

// Skills keep ticking while hidden, so swapping weapons never resets their cooldowns.
public sealed class WeaponSkillLoadout : MonoBehaviour
{
    public WeaponSkillSlot[] swordSkills = new WeaponSkillSlot[3];
    [Tooltip("Assign future bow skill components and their HUD objects here. Their input should call CanUse(this).")]
    public WeaponSkillSlot[] bowSkills = new WeaponSkillSlot[3];
    public UnityEngine.UI.Image sharedSwitchCooldown;
    Player owner;
    int displayedMode = -1;

    void Awake() { owner = GetComponent<Player>(); }
    void Start() { Refresh(); }
    void LateUpdate()
    {
        if (owner != null && displayedMode != owner.weapon_mode) Refresh();
        UpdateSwitchCooldown();
    }

    public void Refresh()
    {
        if (owner == null) owner = GetComponent<Player>();
        if (owner == null) return;
        displayedMode = owner.weapon_mode;
        SetHUD(swordSkills, displayedMode != 0);
        SetHUD(bowSkills, displayedMode == 0);
        UpdateSwitchCooldown();
    }

    void UpdateSwitchCooldown()
    {
        if (owner == null || sharedSwitchCooldown == null) return;
        sharedSwitchCooldown.fillAmount = owner.weaponSwitchCooldown > 0f
            ? Mathf.Clamp01(owner.WeaponSwitchRemaining / owner.weaponSwitchCooldown) : 0f;
    }

    static void SetHUD(WeaponSkillSlot[] slots, bool visible)
    {
        if (slots == null) return;
        foreach (var slot in slots)
        {
            if (slot == null || slot.hud == null) continue;
            var group = slot.hud.GetComponent<CanvasGroup>();
            if (group == null) group = slot.hud.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible && slot.skill != null;
            group.blocksRaycasts = group.interactable;
        }
    }

    public static bool CanUse(MonoBehaviour skill)
    {
        if (skill == null || StageRewardController.InputBlocked || Time.timeScale <= 0f || GameManager.player_current_HP <= 0f) return false;
        var loadout = skill.GetComponentInParent<WeaponSkillLoadout>();
        if (loadout == null)
        {
            var character = GameObject.FindGameObjectWithTag("Player");
            if (character != null) loadout = character.GetComponent<WeaponSkillLoadout>();
        }
        if (loadout == null || loadout.owner == null) return false;
        var player = loadout.owner;
        if (player.on_attack || player.on_dash || player.skill_using) return false;
        var slots = player.weapon_mode == 0 ? loadout.bowSkills : loadout.swordSkills;
        if (slots != null)
            foreach (var slot in slots)
                if (slot != null && slot.skill == skill) return true;
        return false;
    }
}
