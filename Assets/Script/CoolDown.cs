using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoolDown : MonoBehaviour
{
    public Image cooltime;
    public float skillcooltime;
    float currentcooltime;

    bool iscoolingdown = false;



    void Start()
    {
        cooltime.fillAmount = 0;
    }

    void Update()
    {
        if (!iscoolingdown)
        {
            return;
        }
        currentcooltime -= Time.deltaTime;


        if (currentcooltime > 0)
        {
            cooltime.fillAmount = currentcooltime / skillcooltime;
        }
        else
        {
            currentcooltime = 0;
            cooltime.fillAmount = 0;
            iscoolingdown = false;
        }
    }


    public void UseSkill()
    {

        if (!iscoolingdown)
        {
            Debug.Log("ee");
            iscoolingdown=true;
            currentcooltime = skillcooltime;

            cooltime.fillAmount = 1;
        }
    }
}
