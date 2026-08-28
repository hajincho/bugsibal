using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SwordDrop : MonoBehaviour
{

    public List<GameObject> dropswords = new List<GameObject>();

    Transform playertransform;
    Inventory inven;
    public float distance = 3f;
    public GameObject textbox;
    public GameObject background;
    public int swordnum = 0;

    private GameObject currenttextbox;
    private GameObject currentbackground;
    public Item sword;

    void OnEnable()
    {
        playertransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        inven = GameObject.FindWithTag("Player").GetComponent<Inventory>();
    }

    void Update()
    {
        float dis = Vector3.Distance(playertransform.position, transform.position);

        if (dis <= distance)
        {
            if (currenttextbox == null && currentbackground == null)
            {
                currenttextbox = Instantiate(textbox, transform.position + Vector3.up * 1.5f + Vector3.right * 3f, Quaternion.identity);
                currentbackground = Instantiate(background, transform.position + Vector3.up * 1.3f + Vector3.right * 3f, Quaternion.identity);
                currenttextbox.transform.SetParent(transform);
                currentbackground.transform.SetParent(transform);
                TMP_Text txt = currenttextbox.GetComponentInChildren<TMP_Text>();

                txt.alignment = TextAlignmentOptions.Top;
                txt.lineSpacing = 10f;

                txt.text =
                    $"<align=\"center\">{sword.itemName}</align>\n" +
                    $"<align=\"center\">[아이템 줍기 : Q]</align>";

            }
            else
            {
                currenttextbox.transform.position = transform.position + Vector3.up * 1.5f + Vector3.right * 3f;
                currentbackground.transform.position = transform.position + Vector3.up * 1.3f + Vector3.right * 3f;
            }
        }
        else
        {
            if (currenttextbox != null && currentbackground != null)
            {
                Destroy(currenttextbox);
                Destroy(currentbackground);
            }
        }
    }

    public void SwordChange(int swordnum)
    {
        for (int i = 0; i < dropswords.Count; i++)
        {
            dropswords[i].SetActive(false);
        }
        dropswords[swordnum].SetActive(true);
    }
    public void AddItem(Item newitem)
    {
        inven.Inven.Add(newitem);
    }

    public void Pickup()
    {
        if (currenttextbox != null && currentbackground != null)
        {
            Destroy(currenttextbox);
        }
        Destroy(gameObject);
    }
}
