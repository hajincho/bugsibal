using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// The magazine belongs to the weapon profile: switching weapons never refills it.
[DefaultExecutionOrder(26000)]
public sealed class TaksinBowSkill : MonoBehaviour
{
    public GameObject hud;
    public UnityEngine.UI.Image icon, reloadMask;
    public Sprite reloadIcon;
    public TMPro.TMP_Text ammoLabel, statusLabel;
    readonly Dictionary<BowChargeSettings, int> magazines = new Dictionary<BowChargeSettings, int>();
    Player owner;
    BowChargeSettings reloadingProfile;
    BowChargeSettings unlimitedProfile;
    bool hadUnlimitedAmmo;
    float reloadRemaining, nextShotAt;
    Transform aimedWeapon;
    Quaternion restingRotation;
    Vector3 restingScale;
    public BowChargeSettings Profile => owner != null && owner.chargeBow != null ? owner.chargeBow.settings : null;
    public bool IsActiveBow => owner != null && owner.weapon_mode == 0 && Profile != null && Profile.automaticMagazine;
    public int Capacity => Profile != null ? Mathf.Max(1, Profile.magazineCapacity) : 70;
    public int Ammo
    {
        get
        {
            var profile = Profile;
            if (profile == null || !profile.automaticMagazine) return 0;
            int count;
            if (!magazines.TryGetValue(profile, out count)) magazines[profile] = count = Mathf.Max(1, profile.magazineCapacity);
            return Mathf.Clamp(count, 0, Capacity);
        }
    }
    public bool IsReloading => reloadingProfile != null;
    public bool UnlimitedAmmo => IsActiveBow && owner.BowXSkill != null && owner.BowXSkill.IsActive;
    public float ReloadRemaining => IsReloading ? reloadRemaining : 0f;
    public float ShotInterval => 1f / (Mathf.Max(.1f, Profile != null ? Profile.shotsPerSecond : 5f) * (UnlimitedAmmo ? 1.4f : 1f));

    void Awake() { owner = GetComponent<Player>(); }
    void Update()
    {
        SyncBuffState();
        if (UnlimitedAmmo && IsReloading) CancelReload();
        if (IsReloading && (!IsActiveBow || Profile != reloadingProfile || GameManager.player_current_HP <= 0f)) CancelReload();
        if (!IsActiveBow || StageRewardController.InputBlocked || Time.timeScale <= 0f || GameManager.player_current_HP <= 0f) return;
        if (IsReloading)
        {
            reloadRemaining = Mathf.Max(0f, reloadRemaining - Time.deltaTime);
            if (reloadRemaining <= 0f)
            {
                magazines[reloadingProfile] = Mathf.Max(1, reloadingProfile.magazineCapacity);
                reloadingProfile = null;
            }
        }
        if (Input.GetKeyDown(KeyCode.C)) TryReload();
        if (Input.GetMouseButton(0) && Camera.main != null)
            TryFire(Camera.main.ScreenToWorldPoint(Input.mousePosition));
    }

    bool CanAct()
    {
        return isActiveAndEnabled && IsActiveBow && !StageRewardController.InputBlocked && Time.timeScale > 0f &&
            GameManager.player_current_HP > 0f && !owner.on_dash && !owner.skill_using;
    }

    public bool TryFire(Vector3 target)
    {
        SyncBuffState();
        if (!CanAct() || (!UnlimitedAmmo && (IsReloading || Ammo <= 0)) || Time.time < nextShotAt || owner.on_attack ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return false;
        if (UnlimitedAmmo && IsReloading) CancelReload();
        var bow = owner.chargeBow;
        var profile = Profile;
        if (profile.arrowPrefab == null || bow.arrow_location == null) return false;
        AimAt(target);
        var visual = bow.GetComponent<SpriteRenderer>();
        Vector3 origin = visual != null && visual.sprite != null
            ? bow.transform.TransformPoint(new Vector3(visual.sprite.bounds.max.x, .1f, 0f))
            : bow.arrow_location.position;
        target.z = origin.z;
        Vector2 direction = target - origin;
        if (direction.sqrMagnitude < .0001f) direction = Vector2.right;
        var shot = Instantiate(profile.arrowPrefab, origin, Quaternion.identity);
        var projectile = shot.GetComponent<Arrow>();
        if (projectile == null) { Destroy(shot); return false; }
        // Reuse the swept Arrow collision and bow-Q hit handling with a straight bullet prefab.
        float damage = UnlimitedAmmo ? 25f : profile.DamageAt(0f);
        bool piercesEnemies = Random.value < .2f;
        projectile.Fire(origin + (Vector3)direction.normalized, damage, profile.arrowSpeed,
            profile.arrowLifetime, owner.transform, piercesEnemies);
        if (!UnlimitedAmmo) magazines[profile] = Ammo - 1;
        nextShotAt = Time.time + ShotInterval;
        RefreshHUD();
        return true;
    }

    public bool TryReload()
    {
        if (!CanAct() || UnlimitedAmmo || IsReloading || Ammo >= Capacity) return false;
        BeginReload();
        return true;
    }

    void BeginReload()
    {
        reloadingProfile = Profile;
        reloadRemaining = Mathf.Max(.01f, Profile.reloadSeconds);
        RefreshHUD();
    }

    void SyncBuffState()
    {
        bool unlimited = UnlimitedAmmo;
        if (hadUnlimitedAmmo && !unlimited && IsActiveBow && Profile == unlimitedProfile && GameManager.player_current_HP > 0f)
            BeginReload();
        hadUnlimitedAmmo = unlimited;
        unlimitedProfile = unlimited ? Profile : null;
    }

    void CancelReload() { reloadingProfile = null; reloadRemaining = 0f; }
    void OnDisable() { hadUnlimitedAmmo = false; unlimitedProfile = null; CancelReload(); RestoreAim(); if (hud != null) hud.SetActive(false); }
    void LateUpdate()
    {
        if (IsActiveBow && Camera.main != null && Time.timeScale > 0f && !StageRewardController.InputBlocked)
            AimAt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        else if (!IsActiveBow) RestoreAim();
        RefreshHUD();
    }

    public void AimAt(Vector3 target)
    {
        if (!IsActiveBow) return;
        var weapon = owner.chargeBow.transform;
        if (aimedWeapon != weapon)
        {
            RestoreAim();
            aimedWeapon = weapon;
            restingRotation = weapon.localRotation;
            restingScale = weapon.localScale;
        }
        weapon.localScale = Vector3.Scale(restingScale, new Vector3(.8f, -.8f, 1f));
        Vector3 direction = target - weapon.position;
        direction.z = 0f;
        if (direction.sqrMagnitude < .0001f) return;
        // InverseTransformVector also handles the character's mirrored scale.
        Vector3 local = weapon.parent != null ? weapon.parent.InverseTransformVector(direction) : direction;
        weapon.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
    }

    void RestoreAim()
    {
        if (aimedWeapon != null)
        {
            aimedWeapon.localRotation = restingRotation;
            aimedWeapon.localScale = restingScale;
        }
        aimedWeapon = null;
    }
    void RefreshHUD()
    {
        if (hud != null) hud.SetActive(IsActiveBow);
        if (!IsActiveBow) return;
        if (icon != null) icon.sprite = reloadIcon != null ? reloadIcon : Profile.idleSprite;
        if (ammoLabel != null)
        {
            ammoLabel.text = UnlimitedAmmo ? "∞" : Ammo + "/" + Capacity;
            ammoLabel.color = UnlimitedAmmo || Ammo > 0 ? new Color(1f, .9f, .25f) : new Color(1f, .35f, .25f);
        }
        if (statusLabel != null) statusLabel.text = UnlimitedAmmo ? "UNLIMITED" : IsReloading ? "RELOAD " + ReloadRemaining.ToString("0.0") + "s" : Ammo == 0 ? "C RELOAD" : "";
        if (reloadMask != null) reloadMask.fillAmount = IsReloading ? Mathf.Clamp01(ReloadRemaining / Mathf.Max(.01f, Profile.reloadSeconds)) : 0f;
    }
}
