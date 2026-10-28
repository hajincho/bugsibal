using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //player 기본 정보 선언, static으로 지정하여 어디서든 같은 값을 가져올 수 있게 함
    public static int current_stage=-1;

    public static int player_level=0;
    public static int player_speed=8;
    public static int player_power=10;
    public static int player_skill_power = 10;
    public static float player_HP=100;
    public static float player_current_HP=100;

    //스킬 해금 설정
    public static bool skillalbe_1 = false;
    public static bool skillalbe_2 = false;
    public static bool skillalbe_3 = false;
    public static bool skillalbe_4 = false;
    public static bool skillalbe_5 = false;
    public static bool ultable = false;

    public GameObject stored;

    ItemManager itemmanager;
    Player player;


    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        itemmanager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
    }
    void Start()
    {
        Time.timeScale = 1;
        player_current_HP = player_HP;
        
        switch(SceneManager.GetActiveScene().name)
        {
            case "Stage1":
                current_stage=1;
                break;
            case "Stage2":
                current_stage=2;
                break;
            case "Stage3":
                current_stage=3;
                break;
            case "Stage4":
                current_stage=4;
                break;
            case "MidBoss":
                current_stage=10;
                break;
            case "FinalBoss":
                current_stage=20;
                break;

        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Insert))
        {
            if(!PlayerPrefs.HasKey("Stage"))
            {
                PlayerPrefs.SetInt("Stage", current_stage);
                stored.SetActive(true);
                Invoke("StoredDisable", 1);
            }
        }
        if (player_current_HP > player_HP)
        {
            player_current_HP = player_HP;
        }
    }

    void StoredDisable()
    {
        stored.SetActive(false);
    }

    public void AddItemValue()
    {
        Item itemheld = null;
        foreach (Item item in itemmanager.itemList)
        {
            if (item.itemType == ItemType.Weapon && !item.IsBow && item.value == player.sword_num)
            {
                itemheld = item;
                break;
            }
        }
        if (itemheld == null) return;
        float minus_HP = (player_HP - player_current_HP);
        player_HP = itemheld.health + 100 + RunUpgradeState.HealthBonus + (player.midboss_cleared ? 50 : 0);
        player_current_HP = player_HP - minus_HP;
        if (player_current_HP > player_HP)
        {
            player_current_HP = player_HP;
        }
        player_power = itemheld.power + 10 + (player.midboss_cleared ? 7 : 0);
        player_skill_power = itemheld.skillpower + 10;

    }
    public static float DamagePlayer(float amount)
    {
        if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount) || player_current_HP <= 0) return 0;
        float before = player_current_HP;
        player_current_HP = Mathf.Clamp(player_current_HP - amount, 0, player_HP);
        float applied = before - player_current_HP;
        var character = GameObject.FindGameObjectWithTag("Player");
        if (character != null) CombatPresentation.ShowDamage(CombatPresentation.HeadPosition(character.transform), applied, CombatDamageKind.PlayerHit);
        return applied;
    }

    public static float HealPlayer(float amount)
    {
        if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount) || player_current_HP <= 0) return 0;
        float before = player_current_HP;
        player_current_HP = Mathf.Min(player_HP, player_current_HP + amount);
        float healed = player_current_HP - before;
        var character = GameObject.FindGameObjectWithTag("Player");
        if (character != null) CombatPresentation.ShowDamage(CombatPresentation.HeadPosition(character.transform), healed, CombatDamageKind.Heal);
        return healed;
    }

    public static void ResetRun()
    {
        RunUpgradeState.Reset();
        player_level = 0; player_speed = 8; player_power = 10; player_skill_power = 10;
        player_HP = player_current_HP = 100;
        current_stage = -1;
    }
}
