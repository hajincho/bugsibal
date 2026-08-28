using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class drumpokpal : MonoBehaviour
{
    public bool damageable = true;
    public int damage = 5;
    public int player_damage = 15;
    void Start()
    {
        Destroy(gameObject, 1f);
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

            if (!skilldamaged.damaged_skill2)
            {
                enemy_data.enemy_current_HP -= damage;
                enemy_data.Hitted();
                skilldamaged.damaged_skill1 = true;
                
            }
        }
        else if(other.tag == "Player")
        {
            GameManager.player_current_HP -= player_damage;
            other.gameObject.GetComponent<Player>().Hited();
        }
    }
}
