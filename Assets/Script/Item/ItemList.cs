using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string itemName;
    public Sprite icon;
    public int value;
    public int power;
    public int skillpower;
    public int health;
    public int attackSpeed;
    public int duration;
    public int weight;
    public ItemType itemType; // 추가된 필드

    public Item(string name, Sprite icon, int value, int power, int skillpower, int health, int attackSpeed, int duration, int weight, ItemType itemType)
    {
        this.itemName = name;
        this.icon = icon;
        this.value = value;
        this.power = power;
        this.skillpower = skillpower;
        this.health = health;
        this.attackSpeed = attackSpeed;
        this.duration = duration;
        this.weight = weight;
        this.itemType = itemType; // 추가된 초기화 코드
    }
}

public enum ItemType
{
    Weapon,
    Armor,
    Potion
}


