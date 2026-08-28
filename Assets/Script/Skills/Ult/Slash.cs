using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slash : MonoBehaviour
{
    Ultimate ultimate;
    void OnEnable()
    {
        ultimate = GameObject.FindWithTag("Player").GetComponent<Ultimate>();
        Destroy(gameObject, 0.55f);
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Enemy")
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

            if (!skilldamaged.ult2)
            {
                enemy_data.enemy_current_HP -= GameManager.player_skill_power * 2 + GameManager.player_power * 2 + 20;
                enemy_data.Hitted();
                skilldamaged.ult2 = true;
                ultimate.SkillDamageEnable(skilldamaged, 7);
            }
        }
    }
}
