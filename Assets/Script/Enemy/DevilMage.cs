using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class DevilMage : MonoBehaviour
{

    EnemyMove enemyMove;
    Animator animator;
    EnemyData enemyData;
    ItemDrop itemdrop;

    GameObject player;
    public GameObject firepref;
    public GameObject batpref;
    Transform player_transform;
    Player player_script;
    PlayerMove playerMove;

    bool skill1_able = true;
    bool skill2_able = true;
    bool skill3_able = true;
    bool isdead = false;
    float skill1_cooltime = 6.0f;
    float skill2_cooltime = 10.0f;
    float skill3_cooltime = 15.0f;
    public int dropchance = 90;
    
    bool skill_able = true;
    
    float distance;
    Vector3 targetPosition;



    void Start()
    {
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
        int rand = Random.Range(0, 3);
        
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

        if (distance <= enemyMove.enemy_attack_range) // 스킬 사용
        {
            if (skill_able)
            {
                if (skill1_able && rand ==0)//스킬 1(파이어볼)
                {
                    skill_able = false;
                    skill1_able = false;
                    
                    animator.SetInteger("Attack", 3);
                    
                    StartCoroutine(Fireball());
                                      
                    Invoke(nameof(SkillAble), 2.0f);
                    Invoke("ToIdle", 0.68f);
                    StartCoroutine(SkillEnable(1, skill1_cooltime));
                }
                else if (skill2_able && rand == 1)//스킬 2(박쥐 소환)
                {
                    skill_able = false;
                    skill2_able = false;
                    
                    
                    
                    animator.SetInteger("Attack", 2);
                    for (int i = 0; i<2; i++)
                    {
                        GameObject bat = Instantiate(batpref, transform.position, Quaternion.identity);
                    } 
                    
                    Invoke("SkillAble", 2.0f);
                    Invoke("ToIdle", 0.68f);
                    StartCoroutine(SkillEnable(2, skill2_cooltime));
                }
                else if (skill3_able && rand == 2)//스킬 3(적 전체 회복)
                {
                    skill_able = false;
                    skill3_able = false;
                    
                    GameObject[] enemyall = GameObject.FindGameObjectsWithTag("Enemy");
                    foreach (GameObject enemy in enemyall)
                    {
                        if (enemyall != null)
                        {
                            EnemyData enemyData = enemy.GetComponent<EnemyData>();
                            if (enemyData != null)
                            {
                                enemyData.enemy_current_HP = enemyData.enemy_HP;
                            }
                        }
                    }
                    
                    Invoke("SkillAble", 2.0f);
                    Invoke("ToIdle", 0.68f);
                    StartCoroutine(SkillEnable(3, skill3_cooltime));
                }
            }
        }

        
    }
    IEnumerator SkillEnable(int num, float cooltime)
    {
        yield return new WaitForSeconds(cooltime);
        SkillEnable(num);
    }

    IEnumerator Fireball()
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnFireball();
            yield return new WaitForSeconds(0.3f);
        }
    }

    void SkillEnable(int num) // 스킬 쿨타임 초기화
    {
        switch (num)
        {
            case 1:
                skill1_able = true;
                break;
            case 2:
                skill2_able = true;
                break;
            case 3:
                skill3_able = true;
                break;

        }
    }

    void SkillAble() // 스킬 사이 텀
    {
        skill_able = true;
    }

    void SpawnFireball()//파이어볼 소환
    {
        GameObject fireball = Instantiate(firepref, transform.position, Quaternion.identity);
    }

    void ToIdle()//모션 초기화
    {
        animator.SetInteger("Attack", -1);
    }
}


