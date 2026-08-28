using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class heel : MonoBehaviour
{


    public int heal = 10;

    void OnTriggerEnter2D(Collider2D other)
    {

        if (other.gameObject.tag == "Player")
        {
            GameManager.player_current_HP += heal;
            Destroy(gameObject, 0f);
        }

    }
}
