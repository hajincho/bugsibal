using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ItemManager : MonoBehaviour
{
    [Header("0: 힐 포션 / 1: 검 / 2: 활")]
    public List<Item> itemList0 = new List<Item>();
    public List<Item> itemList1 = new List<Item>();
    public List<Item> itemList2 = new List<Item>();
    [FormerlySerializedAs("itemList"), SerializeField, HideInInspector]
    List<Item> legacyItems = new List<Item>();

    // Preserve existing callers without depending on category indexes as weapon IDs.
    public List<Item> itemList
    {
        get
        {
            MigrateLegacyItems();
            var all = new List<Item>(itemList0);
            all.AddRange(itemList1);
            all.AddRange(itemList2);
            return all;
        }
    }

    void Awake() { MigrateLegacyItems(); }
    public void MigrateLegacyItems()
    {
        if (legacyItems == null || legacyItems.Count == 0) return;
        if (itemList0.Count + itemList1.Count + itemList2.Count == 0)
            foreach (var item in legacyItems) Category(item).Add(item);
        legacyItems.Clear();
    }

    List<Item> Category(Item item)
    {
        return item.itemType == ItemType.Potion ? itemList0 : item.IsBow ? itemList2 : itemList1;
    }

    public void AddItem(string itemName, Sprite icon, int value, int power, int skillpower, int health, int attackSpeed, int duration, int weight, ItemType itemType)
    {
        if (GetItem(itemName) != null) return;
        var item = new Item(itemName, icon, value, power, skillpower, health, attackSpeed, duration, weight, itemType);
        Category(item).Add(item);
    }
    public void RemoveItem(string itemName)
    {
        var item = GetItem(itemName);
        if (item != null) Category(item).Remove(item);
    }
    public Item GetItem(string itemName) { return itemList.Find(i => i != null && i.itemName == itemName); }
    public List<Item> GetItemList() { return itemList; }
    public void SortItemsByType(out List<Item> weapons, out List<Item> armors, out List<Item> potions)
    {
        weapons = itemList.FindAll(i => i.itemType == ItemType.Weapon);
        armors = itemList.FindAll(i => i.itemType == ItemType.Armor);
        potions = itemList.FindAll(i => i.itemType == ItemType.Potion);
    }
}
