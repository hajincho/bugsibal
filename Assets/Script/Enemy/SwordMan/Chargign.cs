using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chargign : MonoBehaviour
{
    Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        Destroy(gameObject, 3f);
        animator.speed = 1.1f;
    }
    void Update()
    {

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.normalizedTime >= 0.90f)
        {
            animator.speed = 0f;
        }
    }
}
