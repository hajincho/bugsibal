using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    Rigidbody2D rigid;
    private Transform target;
    private Transform playerTarget;
    private Vector3 offset = new Vector3(0, 1, 0);
    //?? ???, ???? ????, ???? ????
    public float enemy_speed = 3;
    public float enemy_recognition_range = 10;
    public float enemy_attack_range = 1;

    //public float enemy_attack_range=1;
    //???? ???? ?÷???? ??????? ???? ???? ???->??????? ????????? ????
    public bool enemy_attack_range_enabled = false;
    //??? ????? ???????? ??? ??? ???? ????
    public bool moveable = true;
    bool move = false;
    //???? ???? ??? ??????? ?¿????? ??? ???? ??
    float origin_x;
    Animator animator;
    DevilMage rangedMage;
    bool retreating;

    void Start()
    {

        rigid = GetComponent<Rigidbody2D>();
        // ??????? ???
        playerTarget = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        target = playerTarget;
        animator = GetComponent<Animator>();
        origin_x = transform.localScale.x;
        rangedMage = GetComponent<DevilMage>();

        if (!animator)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        if (EnemyFrostStatus.IsFrozenOn(this)) return;
        target = LunarClone.ResolveTarget(transform.position,playerTarget);
        if(target == null)return;
        float distance = Vector2.Distance(transform.position, target.position + offset);
        bool shouldMove = enemy_recognition_range > distance && (enemy_attack_range-1) < distance;
        if(rangedMage!=null)
        {
            float start=Mathf.Max(0f,rangedMage.retreatStartDistance);
            float stop=Mathf.Max(start+.1f,rangedMage.retreatStopDistance);
            retreating=moveable && (retreating ? distance<stop : distance<start);
            shouldMove=enemy_recognition_range>distance && (retreating || distance>Mathf.Max(stop,enemy_attack_range-1));
        }
        if(moveable && shouldMove)
        {
            //???????
            animator.SetBool("Run", true);
            //?? ????????? ???? ?? bool?? 'Run'?? ????? ????? ??? ???????
            move = true;
            //transform.position = Vector2.MoveTowards(transform.position, target.position + offset, enemy_speed*Time.deltaTime);

            //???? ????? ???? ??????? ???????? ???? ????????? scale?? x?? ????? ???? ?????
            if(target.position.x > transform.position.x && moveable)//?÷???? ???? ??????? ???? ??
            {
                transform.localScale = new Vector3(origin_x, transform.localScale.y, transform.localScale.z);//????
            }
            else if(target.position.x < transform.position.x && moveable)//?÷???? ???? ????? ???? ??
            {
                transform.localScale = new Vector3(origin_x * -1, transform.localScale.y, transform.localScale.z);//?¿????
            }
        }
        else
        {
            //????????
            animator.SetBool("Run", false);
            move = false;
            if(rangedMage!=null && rigid!=null)rigid.velocity=Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        if (EnemyFrostStatus.IsFrozenOn(this)) return;
        if(move && target!=null)
        {
            float step=enemy_speed * EnemyFrostStatus.SpeedMultiplier(this) * Time.fixedDeltaTime;
            if(rangedMage!=null && retreating)
            {
                Vector2 away=(Vector2)(transform.position-target.position-offset);
                float stop=Mathf.Max(rangedMage.retreatStartDistance+.1f,rangedMage.retreatStopDistance);
                float remaining=Mathf.Max(0f,stop-away.magnitude);
                if(away.sqrMagnitude<.0001f)away=Vector2.right;
                rigid.MovePosition(rigid.position+away.normalized*Mathf.Min(Mathf.Max(0f,step),remaining));
            }
            else rigid.MovePosition(Vector2.MoveTowards(transform.position, target.position + offset, step));
        }
    }
}
