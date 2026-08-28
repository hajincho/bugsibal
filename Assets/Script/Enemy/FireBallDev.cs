using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class FireBallDev : MonoBehaviour
{
    DevilMage mage;
    Rigidbody2D rigid;
    Vector3 direction;
    public GameObject explosion;
    bool damageable = true;
    float speed = 7f;


    void OnEnable()
    {
        damageable = true;
        Destroy(gameObject, 3f);
        Invoke("SpawnExp", 3f);

        rigid = GetComponent<Rigidbody2D>();
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        direction = (player.transform.position - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        rigid.velocity = direction * speed;
    }

    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            Destroy(gameObject, 0.1f);
            Instantiate(explosion, other.transform.position, Quaternion.identity);
            if (damageable)
            {
                GameManager.player_current_HP -= 15;
            }
            damageable = false;
        }
    }

    void SpawnExp()
    {
        Instantiate(explosion, transform.position, Quaternion.identity);
    }
}
