using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordEffect : MonoBehaviour
{
    Transform playertransform;
    Skill5 skill5;
    PlayerMove playermove;
    bool damageable = true;
    float lifesteal;
    void OnEnable()
    {
        playertransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        skill5 = GameObject.FindWithTag("Player").GetComponent<Skill5>();
        damageable = true;
        Destroy(gameObject, 0.55f);
        playermove = GameObject.FindWithTag("Player").GetComponent<PlayerMove>();
        if (playermove.player_direction == 0)
        {
            transform.localScale = new Vector3(3, 2, 1);
        }
        else if (playermove.player_direction == 1)
        {
            transform.localScale = new Vector3(-3, 2, 1);
        }
    }

    void Update()
    {
        this.transform.position = playertransform.position + new Vector3(0, 1.5f, 0);
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


            if (!skilldamaged.damaged_skill5)
            {
                enemy_data.enemy_current_HP -= GameManager.player_skill_power * 2;
                GameManager.player_current_HP += 15;
                enemy_data.Hitted();
                skilldamaged.damaged_skill5 = true;
                skill5.SkillDamageEnable(skilldamaged);
            }
        }

    }
}

