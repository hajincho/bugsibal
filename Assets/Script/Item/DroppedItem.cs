using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    Rigidbody2D rigidbody;
    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();

        
    }

    void Start()
    {
        ItemMove();

    }

    void ItemMove()
    {
        float direction = Random.Range(-1f, 1f);
        float force = Random.Range(2f, 3f);
        float yforce = Random.Range(4f, 5f);
        rigidbody.AddForce(new Vector2(direction * force, yforce), ForceMode2D.Impulse);
        Invoke("RemoveGravity", 0.8f);
    }

    void RemoveGravity()
    {
        rigidbody.velocity = Vector2.zero;
        rigidbody.gravityScale = 0;
        rigidbody.isKinematic = true;
    }
}
