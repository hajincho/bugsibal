using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    public float lifetime = 1f;       
    public float floatSpeed = 2f;  
    public TextMeshPro textMesh;

    private Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(int damage, int dmgmode)
    {
        textMesh.text = damage.ToString();

        if (dmgmode == 0)
            textColor = Color.cyan;
        else if (dmgmode == 1)
        {
            textColor = new Color(1f, 0.5f, 0f);
        }
        else if (dmgmode == 2)
        {
            textColor = new Color(0.56f, 0.93f, 0.56f);
        }

        textMesh.color = textColor;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        textColor.a -= Time.deltaTime / lifetime;
        textMesh.color = textColor;
    }
}
