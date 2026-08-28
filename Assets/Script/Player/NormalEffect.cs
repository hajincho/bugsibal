using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NormalEffect : MonoBehaviour
{
    Player player;
    PlayerMove playermove;
    Transform playertransform;
    Transform transform;
    Animator animator;
    int dir;
    void OnEnable()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        playermove = GameObject.FindWithTag("Player").GetComponent<PlayerMove>();
        playertransform= GameObject.FindWithTag("Player").GetComponent<Transform>();
        transform = GetComponent<Transform>();
        animator = GetComponent<Animator>();

        animator.speed = 1.6f;
        if (playermove.player_direction == 0)
        {
            transform.localScale = new Vector3(3, 3, 1);
            dir = 1;
        }
        else if (playermove.player_direction == 1)
        {
            transform.localScale = new Vector3(-3, 3, 1);
            dir = -1;
        }
        Destroy(gameObject, 0.33f);
    }

    void Update()
    {
        if (player.sword_num == 10 || player.sword_num == 12)
        {
            transform.position = playertransform.position + new Vector3(2.8f * dir, 1.5f, 1);
        }
        else
        {
            transform.position = playertransform.position + new Vector3(2.3f * dir, 1.5f, 1);
        }
    }
}
