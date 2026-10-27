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
    Player player;
    public float distance = 3f;
    public GameObject textbox;
    public GameObject background;
    public int swordnum = 0;

    private GameObject currenttextbox;
    private GameObject currentbackground;
    public Item sword;
    public bool IsBow => sword != null && sword.IsBow;
    GameObject bowVisual;

    void OnEnable()
    {
        playertransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        inven = GameObject.FindWithTag("Player").GetComponent<Inventory>();
        player = playertransform.GetComponent<Player>();
    }

    void Update()
    {
        if (playertransform == null || sword == null) return;
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

                UpdatePrompt();

            }
            else
            {
                UpdatePrompt();
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

    void UpdatePrompt()
    {
        if (currenttextbox == null || sword == null) return;
        TMP_Text text = currenttextbox.GetComponentInChildren<TMP_Text>();
        if (text == null) return;
        string hint = player != null && player.CanPickupWeapon(sword)
            ? (IsBow ? "[Tab : 활 줍기 / 교체]" : "[Tab : 검 교체]")
            : (IsBow ? "[Shift : 활 모드로 전환]" : "[Shift : 검 모드로 전환]");
        text.text = $"<align=\"center\">{sword.itemName}</align>\n<align=\"center\">{hint}</align>";
    }

    public void Configure(Item item)
    {
        sword = item;
        swordnum = item.value - 1;
        if (!item.IsBow) { SwordChange(swordnum); return; }
        foreach (GameObject visual in dropswords) if (visual != null) visual.SetActive(false);
        if (bowVisual == null)
        {
            bowVisual = new GameObject("BowVisual", typeof(SpriteRenderer));
            bowVisual.transform.SetParent(transform, false);
        }
        bowVisual.transform.localScale = new Vector3(2f, 2f, 1f);
        SpriteRenderer renderer = bowVisual.GetComponent<SpriteRenderer>();
        renderer.sprite = item.icon;
        renderer.sortingOrder = 5;
        bowVisual.SetActive(true);
    }

    public void SwordChange(int swordnum)
    {
        for (int i = 0; i < dropswords.Count; i++)
        {
            dropswords[i].SetActive(false);
        }
        if (bowVisual != null) bowVisual.SetActive(false);
        if (swordnum >= 0 && swordnum < dropswords.Count && dropswords[swordnum] != null)
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
