using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    ItemDrop itemdrop;
    Inventory inventory;

    void Start()
    {
        itemdrop = GetComponent<ItemDrop>();
        inventory = GameObject.FindWithTag("Player").GetComponent<Inventory>();
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            inventory.Inven.Add(itemdrop.Uniquedrop());
            Debug.Log("Drop!");
        }
        Destroy(gameObject);
    }
}
