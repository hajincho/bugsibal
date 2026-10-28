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
    [InspectorName("활 종류별 설정")]
    [Tooltip("활 종류마다 별도 설정 에셋을 연결하세요. 해당 에셋의 최대 차징 시간 (초) 입력칸으로 차징 속도를 조절할 수 있습니다.")]
    public BowChargeSettings bowSettings;
    public bool IsBow => bowSettings != null || (value == 0 && (itemName == "bow" || itemName == "나무 활"));
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


