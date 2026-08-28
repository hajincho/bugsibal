using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class ItemDrop : MonoBehaviour
{
    Inventory Inventory;
    ItemManager itemManager;

    public GameObject healPotion;

    public GameObject sword;

    private Dictionary<Item, int> Itemweights = new Dictionary<Item, int>();
    private Dictionary<Item, int> UniqueItemweights = new Dictionary<Item, int>();
    void Awake()
    {
        Inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
        itemManager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
        foreach (Item item in itemManager.itemList)
        {
            if (!Itemweights.ContainsKey(item))
            {
                Itemweights.Add(item, item.weight);
            }
        }
        foreach (Item item in itemManager.itemList)
        {
            if (!UniqueItemweights.ContainsKey(item) && item.weight <= 50)
            {
                UniqueItemweights.Add(item, item.weight);
            }
        }
    }

    public Item GetItemRandom()
    {
        int totalweight = 0;
        foreach (int weight in Itemweights.Values)
        {
            totalweight += weight;
        }

        if (totalweight <= 0) return null;

        int random = Random.Range(0, totalweight);
        int sum = 0;
        foreach (var item in Itemweights)
        {
            sum += item.Value;
            if (sum > random)
            {
                Debug.Log(item.Key.itemName);
                return item.Key;
            }
        }
        return null;
    }

    public Item Uniquedrop()
    {
        int totalweight = 0;
        foreach (int weight in UniqueItemweights.Values)
        {
            totalweight += weight;
        }

        if (totalweight <= 0) return null;

        int random = Random.Range(0, totalweight);
        int sum = 0;
        foreach (var item in UniqueItemweights)
        {
            sum += item.Value;
            if (sum > random)
            {
                Debug.Log(item.Key.itemName);
                return item.Key;
            }
        }
        return null;
    }

    public void AddItemInventory(Vector3 positionnow)
    {
        Item newitem = GetItemRandom();
        if (newitem.itemName == "Heal")
        {
            PotionInstantiate(positionnow);
            Debug.Log("potionints");
        }
        else
        {
            ItemPrefInst(positionnow, newitem.value, newitem);

        }

        
    }

    public void PotionInstantiate(Vector3 pos)
    {
        Instantiate(healPotion, pos, Quaternion.identity);
    }

    public void ItemPrefInst(Vector3 pos, int value, Item itemsword)
    {
        GameObject droppedsword = Instantiate(sword, pos, Quaternion.identity);
        SwordDrop sworddrop = droppedsword.GetComponent<SwordDrop>();
        sworddrop.swordnum = value - 1;
        sworddrop.sword = itemsword;
        sworddrop.SwordChange(sworddrop.swordnum);
    }
}
