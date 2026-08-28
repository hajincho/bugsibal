using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heal : MonoBehaviour
{

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            GameManager.player_current_HP += 20;
            if (GameManager.player_current_HP > GameManager.player_HP)
            {
                GameManager.player_current_HP = GameManager.player_HP;
            }
            Destroy(gameObject);
        }
    }
}
