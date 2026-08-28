using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pokpal : MonoBehaviour
{
    PokpalSkill skill2;
    public bool damageable = true;
    public int damage = 50;

    void OnEnable()
    {
        skill2 = GameObject.FindWithTag("Player").GetComponent<PokpalSkill>();
    }
    void Start()
    {
        Destroy(gameObject, 0.4f);
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
                    skilldamaged = other.GetComponentInParent<IsSkillDamaged>();
                }

                if (!skilldamaged.damaged_skill2)
                {
                    enemy_data.enemy_current_HP -= GameManager.player_skill_power * 4 + 10;
                    enemy_data.Hitted();
                    skilldamaged.damaged_skill2 = true;
                    skill2.SkillDamageEnable(skilldamaged);
                }
            }
        }
    
}
