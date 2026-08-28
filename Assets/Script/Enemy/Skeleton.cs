using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class Skeleton : MonoBehaviour
{
    EnemyMove enemyMove;
    Animator animator;
    EnemyData enemyData;

    GameObject player;
    Transform player_transform;
    Transform thistransform;
    Player player_script;
    ItemDrop itemdrop;

    float attack_cooltime = 1;
    bool attack_able = true;
    float distance;



    void Start()
    {
        enemyMove = GetComponent<EnemyMove>();
        animator = GetComponentInChildren<Animator>();
        enemyData = GetComponent<EnemyData>();
        player = GameObject.FindGameObjectWithTag("Player");
        player_transform = player.GetComponent<Transform>();
        thistransform = GetComponent<Transform>();
    }


    void Update()
    {
        distance = Vector2.Distance(transform.position, player_transform.position);

        if (enemyData.enemy_current_HP <= 0)
        {
            animator.SetTrigger("Death");
            enemyMove.moveable = false;
            Destroy(gameObject, 0.75f);
        }
    }

    void OnCollisionStay2D(Collision2D other)
    {
        if (distance < 4 && attack_able && other.gameObject.tag == "Player") 
        {
            animator.SetInteger("Attack", 1);
            GameManager.player_current_HP -= enemyData.enemy_power;
            other.gameObject.GetComponent<Player>().Hited();
            attack_able = false;

            Invoke("ToAttackAble", attack_cooltime);
        }
    }

    void ToAttackAble()
    {
        attack_able = true;
        animator.SetInteger("Attack", -1);
    }
}


