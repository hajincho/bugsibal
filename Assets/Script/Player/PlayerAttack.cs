using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    Player player;
    bool has_attacked = false;
    bool projectile;
    float projectileDamage;
    readonly HashSet<EnemyData> struck = new HashSet<EnemyData>();

    void OnEnable()
    {
        var owner = GameObject.FindWithTag("Player");
        player = owner != null ? owner.GetComponent<Player>() : null;
        projectile = GetComponent<Arrow>() != null;
        projectileDamage = RunUpgradeState.NormalPower * 0.5f;
        struck.Clear();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (StageRewardController.InputBlocked) return;
        if (player == null)
        {
            var owner = GameObject.FindWithTag("Player");
            player = owner != null ? owner.GetComponent<Player>() : null;
        }
        if (player == null) return;
        if (other.tag == "Enemy" || other.tag == "drumtong")
        {
            //무기가 적에게 닿으면 공격력만큼 적의 체력 감소
            if(player.on_attack || projectile)//Projectiles retain their launch damage after the attack animation ends.
            {
        
                //검은 데미지 2배
                //활과 데미지가 같으면 원거리공격인 활만씀
                EnemyData enemy_data = other.GetComponent<EnemyData>();
                IsSkillDamaged skilldamaged = other.GetComponent<IsSkillDamaged>();

                if (enemy_data == null)
                {
                    enemy_data = other.GetComponentInParent<EnemyData>();
                }
                if (skilldamaged == null)
                {
                    skilldamaged = other.GetComponentInParent<IsSkillDamaged>();
                }



                if (enemy_data == null) return;
                if (projectile)
                {
                    if (!struck.Add(enemy_data)) return;
                    enemy_data.TakeDamage(projectileDamage, CombatDamageKind.Normal);
                    enemy_data.Hitted();
                    return;
                }
                if (player.weapon_mode == 1 && skilldamaged != null && skilldamaged.damaged_normal) return;

                if (player.weapon_mode == 1)
                {
                    enemy_data.TakeDamage(RunUpgradeState.NormalPower, CombatDamageKind.Normal);
                    Debug.Log("검 공격");
                    if (player.sword_num == 6)
                    {

                        GameManager.HealPlayer(RunUpgradeState.NormalPower * 0.05f);
                    }
                }
                else if(player.weapon_mode == 0)
                {

                    enemy_data.TakeDamage(RunUpgradeState.NormalPower / 2, CombatDamageKind.Normal);
                }

                if (skilldamaged != null)
                {
                    skilldamaged.damaged_normal = true;
                    player.SkillDamageEnable(skilldamaged);
                }
                enemy_data.Hitted();
            }
        }
        
    }

    void ToHasntAttacked()
    {
        has_attacked = false;
    }
}
