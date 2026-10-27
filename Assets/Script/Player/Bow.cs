using UnityEngine;
using UnityEngine.EventSystems;

public class Bow : MonoBehaviour
{
    public Sprite bow_idle;
    public Sprite[] chargeSprites;
    public GameObject arrow;
    public Transform arrow_location;
    public BowChargeSettings settings;
    public BowChargeBar chargeBar;
    public bool IsCharging { get; private set; }
    public float ChargeSeconds { get; private set; }
    public bool IsBowMode => owner != null && owner.weapon_mode == 0;

    Animator animator;
    SpriteRenderer sprite;
    Player owner;
    BowChargeSettings chargingSettings;

    void Awake()
    {
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        owner = GetComponentInParent<Player>();
        if (animator != null) animator.enabled = false;
    }

    public void ApplySettings(BowChargeSettings profile, Sprite icon)
    {
        CancelCharge();
        settings = profile;
        if (profile == null) return;
        if (profile.idleSprite != null) bow_idle = profile.idleSprite;
        else if (icon != null) bow_idle = icon;
        chargeSprites = profile.drawSprites;
        if (profile.arrowPrefab != null) arrow = profile.arrowPrefab;
        if (sprite != null) sprite.sprite = bow_idle;
    }

    public bool BeginCharge()
    {
        if (IsCharging || settings == null || arrow == null || arrow_location == null || owner == null ||
            owner.weapon_mode != 0 || owner.on_dash || owner.skill_using || StageRewardController.InputBlocked ||
            Time.timeScale <= 0f || GameManager.player_current_HP <= 0f ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return false;
        chargingSettings = settings;
        ChargeSeconds = 0f;
        IsCharging = true;
        owner.on_attack = true;
        if (animator != null) animator.enabled = false;
        UpdateVisuals();
        return true;
    }

    void Update()
    {
        if (!IsCharging) return;
        if (owner == null || owner.weapon_mode != 0 || owner.on_dash || owner.skill_using || !owner.on_attack ||
            settings != chargingSettings || StageRewardController.InputBlocked || Time.timeScale <= 0f ||
            GameManager.player_current_HP <= 0f)
        {
            CancelCharge();
            return;
        }
        ChargeSeconds = Mathf.Min(ChargeSeconds + Time.deltaTime, Mathf.Max(0.01f, chargingSettings.maxChargeSeconds));
        UpdateVisuals();
        if (Input.GetMouseButtonUp(0)) Release();
        else if (!Input.GetMouseButton(0)) CancelCharge();
    }

    void UpdateVisuals()
    {
        if (chargeBar != null) chargeBar.Show(ChargeSeconds, chargingSettings);
        if (sprite != null && chargeSprites != null && chargeSprites.Length > 0)
        {
            int index = Mathf.Min(chargeSprites.Length - 1, Mathf.FloorToInt(chargingSettings.ChargeFraction(ChargeSeconds) * chargeSprites.Length));
            sprite.sprite = chargeSprites[index];
        }
    }

    public void Release()
    {
        if (!IsCharging) return;
        if (StageRewardController.InputBlocked || Time.timeScale <= 0f || owner == null || owner.on_dash ||
            owner.skill_using || owner.weapon_mode != 0 || GameManager.player_current_HP <= 0f)
        { CancelCharge(); return; }
        Camera camera = Camera.main;
        if (camera == null || arrow == null || arrow_location == null) { CancelCharge(); return; }
        Vector3 target = camera.ScreenToWorldPoint(Input.mousePosition);
        target.z = arrow_location.position.z;
        GameObject shot = Instantiate(arrow, arrow_location.position, Quaternion.identity);
        Arrow projectile = shot.GetComponent<Arrow>();
        if (projectile != null)
            projectile.Fire(target, chargingSettings.DamageAt(ChargeSeconds), chargingSettings.arrowSpeed,
                chargingSettings.arrowLifetime, owner.transform);
        else Destroy(shot);
        ResetCharge();
        owner.EndBowCharge(true);
    }

    public void CancelCharge()
    {
        bool wasCharging = IsCharging;
        ResetCharge();
        if (wasCharging && owner != null) owner.EndBowCharge(false);
    }

    void ResetCharge()
    {
        IsCharging = false;
        ChargeSeconds = 0f;
        chargingSettings = null;
        if (chargeBar != null)
        {
            if (isActiveAndEnabled && IsBowMode) chargeBar.Show(0f, settings);
            else chargeBar.Hide();
        }
        if (sprite != null && bow_idle != null) sprite.sprite = bow_idle;
        if (animator != null) animator.enabled = false;
    }

    void OnDisable() { CancelCharge(); if (chargeBar != null) chargeBar.Hide(); }
    void OnApplicationFocus(bool focused) { if (!focused) CancelCharge(); }
}
