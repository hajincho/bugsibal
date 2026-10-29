using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    [SerializeField] List<GameObject> weapons = new List<GameObject>();
    [SerializeField] List<GameObject> sword_effects = new List<GameObject>();
    [SerializeField] Animator bow_animator;

    Animator animator;
    GameManager gamemanager;
    ItemManager itemmanager;
    PlayerMove playermove;
    Transform playertransform;
    ItemDrop itemdrop;
    public GameObject trashsword;
    public GameObject hitted_image;

    // 대시 관련 변수
    Vector3 dash_direction;
    public bool on_dash = false;
    public bool dash_able = true;
    public float dash_cooltime = 7.5f;
    public float dash_duration = 0.5f;
    public float dash_time;
    public UnityEngine.UI.Image dash_cool_down;

    // 기타 상태
    public bool on_attack = false;
    public int weapon_mode; // 0: bow, 1: sword (legacy upgraded sword: 2)
    public int sword_num; // 칼 아이템 번호
    public int bow_num; // 활 아이템 번호
    public Bow chargeBow;
    public BowChargeSettings ownedBow;
    [Min(0f)] public float weaponSwitchCooldown = 1f;
    float weaponSwitchReadyAt;
    public float WeaponSwitchRemaining => Mathf.Max(0f, weaponSwitchReadyAt - Time.time);
    Item equippedBowItem;
    public Item EquippedBowItem => equippedBowItem;
    public GameObject sword_effect;
    public bool weakeningable = true;
    public bool midboss_cleared = false;
    public int medicine;

    //스킬 관련 변수
    public bool skill_using = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (chargeBow == null && bow_animator != null) chargeBow = bow_animator.GetComponent<Bow>();
        playertransform = GetComponent<Transform>();
        itemdrop = GetComponent<ItemDrop>();
        gamemanager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
        itemmanager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
        playermove = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMove>();
        hitted_image.SetActive(false);
        dash_able = false;

        weapon_mode = 1;
        sword_num = 1;
        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].SetActive(false);
        }
        weapons[1].SetActive(true);


        animator.SetInteger("Attack", -1);
        animator.SetInteger("Attacked", -1);
    }

    void Start()
    {
        Item sword = itemmanager.GetItem("강철검");
        Item bow = itemmanager.GetItem("나무 활");
        if (sword != null) sword_num = sword.value;
        if (bow != null) EquipBow(bow);
        weapon_mode = 1;
        ApplyWeaponVisibility();
        gamemanager.AddItemValue();
        var inventory = GetComponent<Inventory>();
        if (inventory != null)
        {
            if (sword != null && !inventory.Inven.Contains(sword)) inventory.Inven.Add(sword);
            if (bow != null && !inventory.Inven.Contains(bow)) inventory.Inven.Add(bow);
        }
    }

    void Update()
    {
        if (StageRewardController.InputBlocked) return;
        animator.speed = on_attack && !on_dash && !skill_using && animator.GetInteger("Attacked") == -1 ? RunUpgradeState.AttackRate : 1f;
        // Shift changes weapon category; Tab exchanges a nearby weapon in that category.
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            ToggleWeaponMode();
        if (Input.GetKeyDown(KeyCode.L))
        {
            sword_num += 1;
            //if (weapon_mode == 1)
            //{
                //for (int i = 0; i < weapons.Count; i++)
                //{
                   // weapons[i].SetActive(false);
                //}
               // weapons[sword_num].SetActive(true);
               // gamemanager.AddItemValue();
            //}
           // Debug.Log(sword_num);
        }
        if (sword_num <= 0 || sword_num >= weapons.Count)
        {
            sword_num = 1;
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log(sword_num + ":" + GameManager.player_HP);
            Debug.Log(sword_num + ":" +  GameManager.player_current_HP);
            Debug.Log(sword_num + ":" + GameManager.player_power);
            Debug.Log(sword_num + ":" + GameManager.player_skill_power);
        }


        // 공격 처리
        if (Input.GetMouseButtonDown(0) && !on_attack && !on_dash && !skill_using)
        {
            on_attack = true;

            switch (weapon_mode)
            {
                case 1:
                    //sword_effects[sword_num -1].SetActive(true);
                    animator.SetInteger("Attack", 0);
                    if (playermove.player_direction == 1) 
                    {
                        StartCoroutine(InstEffect(1,0.2f ));
                    }
                    else if (playermove.player_direction == 0)
                    {
                        StartCoroutine(InstEffect(-1, 0.2f));
                    }
                    FlipTowardMoveInput();
                    animator.speed = RunUpgradeState.AttackRate;
                    Invoke(nameof(ToIdle), 0.68f / RunUpgradeState.AttackRate);
                    break;

                case 0:
                    if (chargeBow == null || !chargeBow.BeginCharge()) on_attack = false;
                    break;
            }
        }

        // 대시 입력 (WASD 방향)
        if (Input.GetKeyDown(KeyCode.Space) && dash_able)
        {
            Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            if (inputDir == Vector2.zero) return; // 입력이 없으면 대시 안 함

            if (chargeBow != null) chargeBow.CancelCharge();
            dash_direction = inputDir;

            dash_able = false;
            on_dash = true;
            dash_time = 0f;

            on_attack = false;
            PlayerMove.moveable = false;
            bow_animator.enabled = false;
            animator.SetTrigger("Dash");
            dash_cool_down.fillAmount = 1f;

            FlipTowardMoveInput();
            StartCoroutine(Dash());
        }

        if (Input.GetKeyDown(KeyCode.Tab)) TryPickupNearestWeapon();
    }

    public void ResetWeaponModeAfterRevive()
    {
        if (chargeBow != null) chargeBow.CancelCharge();
        CancelInvoke(nameof(ToIdle));
        weapon_mode = 1;
        on_attack = false;
        animator.SetInteger("Attack", -1);
        animator.speed = 1f;
        var bowSkill = GetComponent<BowExplosiveArrowSkill>();
        if (bowSkill != null) bowSkill.CancelArmed();
        var icicleSkill = GetComponent<BowIcicleSkill>();
        if (icicleSkill != null) icicleSkill.ResetHealingHits();
        ApplyWeaponVisibility();
        gamemanager.AddItemValue();
    }

    public bool ToggleWeaponMode()
    {
        if (WeaponSwitchRemaining > 0f) return false;
        if (StageRewardController.InputBlocked || on_dash || skill_using || GameManager.player_current_HP <= 0f) return false;
        if (on_attack && (chargeBow == null || !chargeBow.IsCharging)) return false;
        if (weapon_mode != 0 && ownedBow == null) return false;
        if (chargeBow != null) chargeBow.CancelCharge();
        CancelInvoke(nameof(ToIdle));
        on_attack = false;
        animator.SetInteger("Attack", -1);
        animator.speed = 1f;
        weapon_mode = weapon_mode == 0 ? 1 : 0;
        weaponSwitchReadyAt = Time.time + weaponSwitchCooldown;
        ApplyWeaponVisibility();
        if (weapon_mode == 1) gamemanager.AddItemValue();
        return true;
    }

    void ApplyWeaponVisibility()
    {
        var loadout = GetComponent<WeaponSkillLoadout>();
        if (loadout != null) loadout.Refresh();
        int selected = weapon_mode == 0 ? 0 : sword_num;
        for (int i = 0; i < weapons.Count; i++)
            if (weapons[i] != null) weapons[i].SetActive(i == selected);
        if (chargeBow != null && chargeBow.chargeBar != null)
        {
            if (weapon_mode == 0) chargeBow.chargeBar.Show(0f, chargeBow.settings);
            else chargeBow.chargeBar.Hide();
        }
    }

    public bool CanPickupWeapon(Item item)
    {
        if (item == null || item.itemType != ItemType.Weapon) return false;
        if (item.IsBow)
            return item.bowSettings != null && weapon_mode == 0;
        return weapon_mode != 0 && item.value > 0 && item.value < weapons.Count;
    }

    public bool TryPickupNearestWeapon()
    {
        return TryPickupWeapon(FindNearestSwordDrop());
    }

    public bool TryPickupWeapon(SwordDrop drop)
    {
        if (drop == null || StageRewardController.InputBlocked || on_attack || on_dash || skill_using ||
            GameManager.player_current_HP <= 0f || !CanPickupWeapon(drop.sword) ||
            Vector3.Distance(transform.position, drop.transform.position) >= drop.distance) return false;
        Item next = drop.sword;
        Item previous = null;
        if (next.IsBow)
        {
            previous = equippedBowItem;
            if (previous == null && ownedBow != null)
                previous = itemmanager.itemList.Find(i => i.IsBow && i.value == bow_num && i.bowSettings == ownedBow);
            if (!EquipBow(next)) return false;
        }
        else
        {
            previous = itemmanager.itemList.Find(i => !i.IsBow && i.itemType == ItemType.Weapon && i.value == sword_num);
            sword_num = next.value;
            weapon_mode = 1;
            gamemanager.AddItemValue();
            ApplyWeaponVisibility();
        }
        if (previous != null) itemdrop.ItemPrefInst(transform.position, previous.value, previous);
        Inventory inventory = GetComponent<Inventory>();
        if (inventory != null)
        {
            if (previous != null) inventory.Inven.Remove(previous);
            if (!inventory.Inven.Contains(next)) inventory.Inven.Add(next);
        }
        drop.Pickup();
        return true;
    }

    // Developer inventory equips directly without consuming a ground drop.
    public bool EquipDeveloperWeapon(Item item)
    {
        if (item == null || item.itemType != ItemType.Weapon || on_dash || skill_using ||
            GameManager.player_current_HP <= 0f) return false;
        if (item.IsBow)
        {
            if (!EquipBow(item)) return false;
        }
        else
        {
            if (item.value <= 0 || item.value >= weapons.Count || weapons[item.value] == null) return false;
            if (chargeBow != null) chargeBow.CancelCharge();
            CancelInvoke(nameof(ToIdle));
            on_attack = false;
            animator.SetInteger("Attack", -1);
            animator.speed = 1f;
            sword_num = item.value;
            weapon_mode = 1;
            gamemanager.AddItemValue();
            ApplyWeaponVisibility();
        }
        var inventory = GetComponent<Inventory>();
        if (inventory != null && !inventory.Inven.Contains(item)) inventory.Inven.Add(item);
        return true;
    }

    public bool EquipBow(Item item)
    {
        if (item == null || !item.IsBow || item.bowSettings == null || chargeBow == null) return false;
        chargeBow.CancelCharge();
        CancelInvoke(nameof(ToIdle));
        equippedBowItem = item;
        ownedBow = item.bowSettings;
        bow_num = item.value;
        chargeBow.ApplySettings(ownedBow, item.icon);
        gamemanager.AddItemValue();
        weapon_mode = 0;
        on_attack = false;
        animator.SetInteger("Attack", -1);
        ApplyWeaponVisibility();
        return true;
    }

    public void EndBowCharge(bool fired)
    {
        CancelInvoke(nameof(ToIdle));
        on_attack = fired;
        animator.speed = 1f;
        animator.SetInteger("Attack", fired ? 1 : -1);
        if (fired) Invoke(nameof(ToIdle), 0.2f);
    }

    SwordDrop FindNearestSwordDrop() //가장 가까운 드롭 아이템 찾기
    {
        SwordDrop[] drops = FindObjectsOfType<SwordDrop>();
        SwordDrop nearest = null;
        float minDist = Mathf.Infinity;

        foreach (SwordDrop drop in drops)
        {
            if (!CanPickupWeapon(drop.sword)) continue;
            float dist = Vector3.Distance(transform.position, drop.transform.position);
            if (dist < drop.distance && dist < minDist)
            {
                minDist = dist;
                nearest = drop;
            }
        }
        return nearest;
    }

    public void FlipTowardMoveInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal > 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (horizontal < 0)
            transform.localScale = new Vector3(1, 1, 1);
    }

    void ToIdle()
    {
        animator.speed = 1f;
        if (bow_animator != null) bow_animator.speed = 1f;
        animator.SetInteger("Attack", -1);
        on_attack = false;
        PlayerMove.moveable = animator.GetInteger("Attacked") == -1;
        //sword_effects[sword_num - 1].SetActive(false );

    }

    public void Attacked(int Attacked)
    {
        // Rat fear stun still stops movement, but it does not interrupt drawing/firing a bow.
        if (chargeBow != null && !(Attacked == 1 && weapon_mode == 0)) chargeBow.CancelCharge();
        if (!on_dash)
        {
            animator.SetInteger("Attacked", Attacked);
            PlayerMove.moveable = false;
            Invoke(nameof(ReMove), Attacked == 0 ? 0.2f : 1f);
        }
    }

    void ReMove()
    {
        animator.SetInteger("Attacked", -1);
        PlayerMove.moveable = true;
    }

    IEnumerator Dash()
    {
        float dashElapsed = 0f;
        float dashSpeed = 20f;

        while (dashElapsed < dash_duration)
        {
            transform.position += dash_direction * dashSpeed * Time.deltaTime;
            dashElapsed += Time.deltaTime;
            yield return null;
        }

        on_dash = false;
        ReMove();

        while (dash_time < dash_cooltime)
        {
            dash_time += Time.deltaTime;
            dash_cool_down.fillAmount = 1f - (dash_time / dash_cooltime);
            yield return null;
        }

        dash_cool_down.fillAmount = 0f;
        dash_able = true;
    }

    public void Hited()
    {
        hitted_image.SetActive(true);
        Invoke(nameof(ToOriginColor), 0.3f);
    }

    void ToOriginColor()
    {
        hitted_image.SetActive(false);
    }

    public void WeaponUpgrade()
    {
        midboss_cleared = true;
        GameManager.player_power += 7;
        GameManager.player_HP += 50;
        GameManager.HealPlayer(50);
        weapons[0].SetActive(false);

        switch (weapon_mode)
        {
            case 0:
                weapon_mode = 2;
                weapons[2].SetActive(true);
                weapons[1].SetActive(false);
                break;
            case 1:
                weapon_mode = 1;
                weapons[2].SetActive(false);
                weapons[1].SetActive(true);
                break;
        }
    }
    public void Heal(int amount)
    {
        GameManager.HealPlayer(amount);
    }

    



    IEnumerator InstEffect(int dir, float time)
    {
        yield return new WaitForSeconds(time / RunUpgradeState.AttackRate);
        if (sword_num == 10)
        {
            Instantiate(sword_effects[sword_num - 1], playertransform.position + new Vector3(-2.8f * dir, 1.5f, 1), Quaternion.identity);
        }
        else
        {
            Instantiate(sword_effects[sword_num - 1], playertransform.position + new Vector3(-2.3f * dir, 1.5f, 1), Quaternion.identity);
        }

    }

    public void SkillDamageEnable(IsSkillDamaged skilldamaged)//적이 공격당했는지 여부 초기화
    {
        StartCoroutine(Skill(skilldamaged,0.4f / RunUpgradeState.AttackRate, 0));
    }
    IEnumerator Skill(IsSkillDamaged skilldamaged, float delay, int skillnum)
    {
        yield return new WaitForSeconds(delay);
        skilldamaged.SkillAble(skillnum);
    }
}
