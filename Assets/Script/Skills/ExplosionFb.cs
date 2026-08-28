using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionFb : MonoBehaviour
{
    Skill1 skill1;
    public bool damageable_exp = false;
    void OnEnable()
    {
        damageable_exp = true;
        skill1 = GameObject.FindWithTag("Player").GetComponent<Skill1>();
        Destroy(gameObject, 0.55f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Enemy" || other.tag == "drumtong")
        {
            EnemyData enemy_data = other.GetComponent<EnemyData>();
            IsSkillDamaged skilldamaged = other.GetComponent<IsSkillDamaged>();
            if (enemy_data == null)
            {
                enemy_data = other.GetComponentInParent<EnemyData>();
            }
            
            if (skilldamaged == null)
            {
                skilldamaged= other.GetComponentInParent<IsSkillDamaged>();
            }

            if (!skilldamaged.damaged_skill1)
            {
                enemy_data.enemy_current_HP -= GameManager.player_skill_power + 20;
                enemy_data.Hitted();
                skilldamaged.damaged_skill1 = true;
                skill1.SkillDamageEnable(skilldamaged);
            }
        }
    }
}
