using UnityEngine;

public sealed class JudgementModeSkill : MonoBehaviour
{
    public BowChargeSettings[] modes = new BowChargeSettings[4];
    public Sprite[] modeIcons = new Sprite[4];
    public GameObject hud;
    public UnityEngine.UI.Image icon;
    public UnityEngine.UI.Image cooldownMask;
    public TMPro.TextMeshProUGUI modeLabel;
    static readonly string[] RomanStages = { "I", "II", "III", "IV" };
    [Min(.001f)] public float cooldownSeconds = .2f;
    public int Mode { get; private set; } = 1;
    public float RemainingCooldown => Mathf.Max(0f, readyAt - Time.time);
    float readyAt;
    Player owner;
    public bool IsEquipped => owner != null && owner.EquippedBowItem != null &&
        modes != null && modes.Length == 4 && owner.EquippedBowItem.bowSettings == modes[0];

    void Awake() { owner = GetComponent<Player>(); }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C)) TryCycle();
    }
    void LateUpdate() { RefreshHUD(); }
    public void ResetForEquip(Item item)
    {
        Mode = 1;
        // Picking up the bow resets the form; switching sword/bow categories does not.
        readyAt = 0f;
        RefreshHUD();
    }
    public bool TryCycle()
    {
        if (!IsEquipped || owner.weapon_mode != 0 || StageRewardController.InputBlocked ||
            Time.timeScale <= 0f || GameManager.player_current_HP <= 0f || RemainingCooldown > 0f ||
            owner.on_dash || owner.skill_using) return false;
        int next = Mode % 4 + 1;
        if (modes[next-1] == null || owner.chargeBow == null) return false;
        owner.chargeBow.CancelCharge();
        Mode = next;
        owner.ownedBow = modes[Mode-1];
        owner.chargeBow.ApplySettings(owner.ownedBow, owner.EquippedBowItem.icon);
        if(owner.chargeBow.chargeBar != null) owner.chargeBow.chargeBar.Show(0f, owner.ownedBow);
        readyAt = Time.time + cooldownSeconds;
        RefreshHUD();
        return true;
    }
    void RefreshHUD()
    {
        if(owner == null) owner = GetComponent<Player>();
        bool visible = IsEquipped && owner.weapon_mode == 0;
        if(hud != null) hud.SetActive(visible);
        if(icon != null && modeIcons != null && modeIcons.Length >= Mode) icon.sprite = modeIcons[Mode-1];
        if(modeLabel != null) modeLabel.text = RomanStages[Mathf.Clamp(Mode-1,0,3)];
        if(cooldownMask != null) cooldownMask.fillAmount = cooldownSeconds > 0f ?
            Mathf.Clamp01(RemainingCooldown / cooldownSeconds) : 0f;
    }
}
