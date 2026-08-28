using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Devil1 : MonoBehaviour
{

    EnemyMove enemyMove;
    Animator animator;
    EnemyData enemyData;
    ItemDrop itemdrop;

    public GameObject dust;
    GameObject player;
    Transform player_transform;
    Player player_script;
    PlayerMove playerMove;

    float attack_cooltime = 0.77f;
    bool attack_able = true;
    bool reinforced_attack = false;
    bool teleport_able = false;
    bool isdead = false;
    float distance;
    public int dropchance = 70;

    Vector3 targetPosition;



    void Start()
    {
        Invoke("TeleportAble", 0.7f);
        enemyMove = GetComponent<EnemyMove>();
        animator = GetComponentInChildren<Animator>();
        enemyData = GetComponent<EnemyData>();
        player = GameObject.FindGameObjectWithTag("Player");
        player_transform = player.GetComponent<Transform>();
        playerMove = player.GetComponent<PlayerMove>();
        itemdrop = GetComponent<ItemDrop>();
    }


    void Update()
    {
        distance = Vector2.Distance(transform.position, player_transform.position);

        if (enemyData.enemy_current_HP <= 0) // 사망
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
            Destroy(gameObject, 1f);
        }

        if (teleport_able) // 플레이어 뒤로 텔레포트
        {
            
            teleport_able = false;
            reinforced_attack = true;
            if (playerMove.player_direction == 0) // 텔레포트 방향 설정
            {
                targetPosition = player_transform.position - (Vector3)Vector2.right * 1.5f;
            }
            else if (playerMove.player_direction == 1)
            {
                targetPosition = player_transform.position - (Vector3)Vector2.left * 1.5f;
            }
           
            transform.position = targetPosition;
            Instantiate(dust, transform.position, Quaternion.identity);//텔레포트 이펙트


            Invoke("TeleportAble", 10f);
        }
    }

    void OnCollisionStay2D(Collision2D other) //충돌시 공격
    {
        if (distance < 4 && attack_able && other.gameObject.tag == "Player") 
        {
            if (reinforced_attack)//텔레포트 후 강화공격
            {
                animator.SetInteger("Attack", 3);
                GameManager.player_current_HP -= enemyData.enemy_power * 3;
                other.gameObject.GetComponent<Player>().Hited();
                attack_able = false;
                reinforced_attack=false;

                Invoke("ToAttackAble", attack_cooltime);
            }
            else
            {
                animator.SetInteger("Attack", 0);
                GameManager.player_current_HP -= enemyData.enemy_power;
                other.gameObject.GetComponent<Player>().Hited();
                attack_able = false;

                Invoke("ToAttackAble", attack_cooltime);
            }
        }
    }

    void ToAttackAble()//공격 초기화
    {
        attack_able = true;
        animator.SetInteger("Attack", -1);
    }

    void TeleportAble()//텔레포트 초기화
    {
        teleport_able = true;
    }



}

