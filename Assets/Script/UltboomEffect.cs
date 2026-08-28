using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltboomEffect : MonoBehaviour
{
    Animator animator;
    void OnEnable()
    {
        animator = GetComponent<Animator>();
        animator.speed = 0.7f;
        Destroy(gameObject, 0.7f);
    }
}
