using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HPNum : MonoBehaviour
{
    public Text hpText;

    void Update()
    {
        hpText.text = GameManager.player_current_HP.ToString() + " / " + GameManager.player_HP.ToString();
    }
}