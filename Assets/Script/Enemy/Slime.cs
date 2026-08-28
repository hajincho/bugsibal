using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slime : MonoBehaviour
{

    EnemyMove enemyMove;
    Animator animator;
    EnemyData enemyData;
    ItemDrop itemdrop;

    GameObject player;
    Transform player_transform;
    Player player_script;

    float attack_cooltime = 1;
    bool attack_able = true;
    float skill_cooltime = 8;
    bool skill_able = true;
    bool isdead = false;
    float distance;
    public int dropchance = 50;



    void Start()
    {
        enemyMove = GetComponent<EnemyMove>();
        animator = GetComponent<Animator>();
        enemyData = GetComponent<EnemyData>();
        player = GameObject.FindGameObjectWithTag("Player");
        player_transform = player.GetComponent<Transform>();
        player_script = player.GetComponent<Player>();
        itemdrop = GetComponent<ItemDrop>();
    }


    void Update()
    {
        distance = Vector2.Distance(transform.position, player_transform.position);

        if (enemyData.enemy_current_HP <= 0)
        {
            animator.SetTrigger("Death");
            if (!isdead)
            {
                int random = Random.Range(0, 10);
                if (random <= (dropchance - 10) / 10)
                {
                    itemdrop.AddItemInventory(this.transform.position);

                }
                else
                {
                    Debug.Log("Fail");
                }
                isdead = true;
            }
            enemyMove.moveable = false;
            Destroy(gameObject, 1);
        }
    }

    void OnCollisionStay2D(Collision2D other)//�����ϴ� ���� ����
    {
        if (distance < 4 && skill_able && other.gameObject.tag == "Player" && player_script.weakeningable) // ��ų
        {
            animator.SetTrigger("Ability");
            GameManager.player_current_HP -= 2* enemyData.enemy_power;
            other.gameObject.GetComponent<Player>().Hited();
            player_script.Attacked(1); //����
            GameManager.player_power /= 2; // ���ݷ� ����
            player_script.weakeningable = false;
            Debug.Log("skill");
            Debug.Log(GameManager.player_current_HP);
            Debug.Log(GameManager.player_power);
            
            skill_able = false;

            Invoke("ToSkillAble", skill_cooltime);
            Invoke("WeakeningDisable", 3);

        }
        else if (distance < 4 && attack_able && other.gameObject.tag == "Player") //����
        {
            animator.SetTrigger("Attack");
            GameManager.player_current_HP -= enemyData.enemy_power;
            other.gameObject.GetComponent<Player>().Hited();
            Debug.Log("attack");
            Debug.Log(GameManager.player_current_HP);

            attack_able = false;

            Invoke("ToAttackAble", attack_cooltime);
        }
    }

    void ToAttackAble()
    {
        attack_able = true;
    }

    void ToSkillAble()
    {
        skill_able = true;
    }

    void WeakeningDisable()
    {
        GameManager.player_power *= 2;
        Debug.Log(GameManager.player_power);
        player_script.weakeningable = true;
    }
}
