using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    Player player;
    bool has_attacked = false;
    bool projectile;

    void OnEnable()
    {
        var owner = GameObject.FindWithTag("Player");
        player = owner != null ? owner.GetComponent<Player>() : null;
        projectile = GetComponent<Arrow>() != null;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        // Arrow owns its launch damage and consumes the entire projectile on its first hit.
        if (projectile || StageRewardController.InputBlocked) return;
        if (player == null)
        {
            var owner = GameObject.FindWithTag("Player");
            player = owner != null ? owner.GetComponent<Player>() : null;
        }
        if (player == null) return;
        if (other.tag == "Enemy" || other.tag == "drumtong")
        {
            //무기가 적에게 닿으면 공격력만큼 적의 체력 감소
            if(player.on_attack)
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



                if (enemy_data == null || enemy_data.enemy_current_HP <= 0f) return;
                if (player.weapon_mode == 1 && skilldamaged != null && skilldamaged.damaged_normal) return;

                if (player.weapon_mode == 1)
                {
                    enemy_data.TakeDamage(RunUpgradeState.NormalPower, CombatDamageKind.Normal);
                    Debug.Log("검 공격");
                    // Fixed raw HP per valid enemy hit; never scale this with attack power.
                    if (player.sword_num == 12 && other.CompareTag("Enemy"))
                        GameManager.HealPlayer(20f);
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
