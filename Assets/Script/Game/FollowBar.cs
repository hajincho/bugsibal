using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FollowBar : MonoBehaviour
{
    public Transform player;
    public Vector3 offset;

    void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Transform>();    
    }
    void Update()
    {
        transform.position = player.position + new Vector3(0, 1f, 0);
    }
}
