using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    float speed = 25f;
    int num;
    Vector3 direction;
    Ultimate ultimate;

    void OnEnable()
    {
        ultimate = GameObject.FindWithTag("Player").GetComponent<Ultimate>();
        Destroy(gameObject, 3f);
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

            if (!skilldamaged.ult3)
            {
                enemy_data.enemy_current_HP -= GameManager.player_skill_power + GameManager.player_power + 30;
                enemy_data.Hitted();
                skilldamaged.ult3 = true;
                ultimate.SkillDamageEnable(skilldamaged, 8);
            }
        }
    }
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
    public void SetDirection(Vector3 dir, int newnum)
    {
        direction = dir.normalized;
        num = newnum;
        switch (num)
        {
            case 0:
                break;
            case 1:
                direction = Quaternion.Euler(0, 0, 8f) * direction;
                break;
            case 2:
                direction = Quaternion.Euler(0, 0, -8f) * direction;
                break;

        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
}
