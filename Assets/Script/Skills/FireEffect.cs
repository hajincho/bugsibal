using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FireEffect : MonoBehaviour
{
    Transform player_transform;
    GameObject player;
    Skill1 skill;


    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        player_transform = player.GetComponent<Transform>();
        skill = player.GetComponent<Skill1>();
        Vector2 ypos;
    }
    public void VanishingFire()
    {
    }

    void Update()
    {
        if (!skill.skill_ready)
        {
            Destroy(gameObject);
        }
        Vector3 pos = player_transform.position;
        pos.y -= 0.4f;
        transform.position = pos;
    }
}
