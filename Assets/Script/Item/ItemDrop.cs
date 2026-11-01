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
    public static int KillsWithoutWeapon { get; private set; }
    bool enemyDeathResolved;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetWeaponDropStreak(){KillsWithoutWeapon=0;}

    public static void ResolveEnemyDeath(EnemyData enemy)
    {
        if(enemy==null || !enemy.CompareTag("Enemy") || enemy.GetComponent<ExplosiveBarrel>()!=null || enemy.GetComponent<TrainingDummy>()!=null)return;
        if(enemy.WeaponDropResolved)return;
        enemy.WeaponDropResolved=true;
        var local=enemy.GetComponent<ItemDrop>();
        if(local!=null && local.enemyDeathResolved)return;
        if(local!=null)local.enemyDeathResolved=true;
        // Golem already has a guaranteed rare-weapon reward in its death sequence.
        if(enemy.GetComponent<Golem>()!=null){KillsWithoutWeapon=0;return;}
        ItemDrop source=local;
        if(source==null){var player=GameObject.FindGameObjectWithTag("Player");if(player!=null)source=player.GetComponent<ItemDrop>();}
        if(source==null)return;
        int chance=0;
        foreach(var component in enemy.GetComponents<MonoBehaviour>())
        {
            if(component==null)continue;
            var field=component.GetType().GetField("dropchance");
            if(field!=null && field.FieldType==typeof(int)){chance=(int)field.GetValue(component);break;}
        }
        // Match the old ten-step enemy roll; only the weapon result is reduced to 60%.
        float oldChance=Mathf.Clamp(((chance-10)/10)+1,0,10)/10f;
        Item chosen=Random.value<oldChance?source.GetItemRandom():null;
        bool weaponDropped=false;
        if(chosen!=null)
        {
            if(chosen.itemName=="Heal")source.PotionInstantiate(enemy.transform.position);
            else if(chosen.itemType==ItemType.Weapon && Random.value<.6f)
                weaponDropped=source.ItemPrefInst(enemy.transform.position,chosen.value,chosen);
        }
        if(weaponDropped){KillsWithoutWeapon=0;return;}
        KillsWithoutWeapon++;
        if(KillsWithoutWeapon<6)return;
        int total=0;foreach(var entry in source.Itemweights)if(entry.Key.itemType==ItemType.Weapon)total+=entry.Value;
        if(total<=0)return;
        int roll=Random.Range(0,total);
        foreach(var entry in source.Itemweights)
        {
            if(entry.Key.itemType!=ItemType.Weapon)continue;
            roll-=entry.Value;if(roll>=0)continue;
            if(source.ItemPrefInst(enemy.transform.position,entry.Key.value,entry.Key))KillsWithoutWeapon=0;
            break;
        }
    }
    void Awake()
    {
        Inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
        itemManager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
        foreach (Item item in itemManager.itemList)
        {
            if (item.weight > 0 && !Itemweights.ContainsKey(item))
            {
                Itemweights.Add(item, item.weight);
            }
        }
        foreach (Item item in itemManager.itemList)
        {
            if (!UniqueItemweights.ContainsKey(item) && item.itemType == ItemType.Weapon &&
                item.weight > 0 && item.weight <= 50 &&
                item.itemName != "아르테미시아" && item.itemName != "사랑의 이름으로" && item.itemName != "정의의 이름으로")
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
        if(enemyDeathResolved)return;
        Item newitem = GetItemRandom();
        if (newitem == null) return;
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

    public bool ItemPrefInst(Vector3 pos, int value, Item itemsword)
    {
        if (itemsword == null) return false;
        GameObject template = sword;
        if (template == null)
        {
            Player owner = GetComponent<Player>();
            if(owner==null)owner=FindObjectOfType<Player>();
            if (owner != null) template = owner.trashsword;
        }
        if (template == null) return false;
        GameObject droppedsword = Instantiate(template, pos, Quaternion.identity);
        SwordDrop sworddrop = droppedsword.GetComponent<SwordDrop>();
        sworddrop.Configure(itemsword);
        return true;
    }
}
