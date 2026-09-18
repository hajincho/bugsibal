using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    float moveX;
    float moveY;
    Rigidbody2D rigid;
    Animator animator;
    Transform player_transform;
    public static bool moveable;
    public int player_direction = 1;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player_transform = GetComponent<Transform>();
        moveable = true;
    }

    void Update()
    {
        // WASD 이동 입력
        moveX = Input.GetAxisRaw("Horizontal");
        moveY = Input.GetAxisRaw("Vertical");

        // 움직일 때 애니메이션 작동
        if ((moveX != 0 || moveY != 0) && animator.GetInteger("Attack") == -1 && animator.GetInteger("Attacked") == -1 && moveable)
        {
            animator.SetBool("Run", true);
        }
        else
        {
            animator.SetBool("Run", false);
        }

        // --- [마우스 방향 전환 로직 시작] ---
        if (moveable && animator.GetInteger("Attack") == -1 && animator.GetInteger("Attacked") == -1)
        {
            // 1. 화면의 마우스 좌표를 월드 게임 좌표로 변환
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // 2. 마우스 x위치가 캐릭터 x위치보다 오른쪽에 있는지 비교
            if (mousePosition.x > transform.position.x)
            {
                // 오른쪽 바라보기 (기존 스프라이트 원본이 오른쪽을 바라볼 경우: -1 또는 1 적용)
                player_transform.localScale = new Vector3(-1, 1, 1);
                player_direction = 0;
            }
            else if (mousePosition.x < transform.position.x)
            {
                // 왼쪽 바라보기
                player_transform.localScale = new Vector3(1, 1, 1);
                player_direction = 1;
            }
        }
        // --- [마우스 방향 전환 로직 끝] ---
    }

    void FixedUpdate()
    {
        if (moveable)
            rigid.velocity = new Vector2(moveX, moveY).normalized * GameManager.player_speed;
        else
            rigid.velocity = Vector2.zero;
    }
}