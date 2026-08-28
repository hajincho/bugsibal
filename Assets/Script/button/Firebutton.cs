using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    public Image cooldownImage;       // 쿨타임 이미지 (Fill 방식)
    public float cooldownTime = 5f;   // 쿨타임 시간

    private bool isCooldown = false;
    private float cooldownTimer = 0f;

    void Start()
    {
        cooldownImage.fillAmount = 0f; // 처음엔 쿨타임 없음
    }

    void Update()
    {
        // 1번 키를 눌렀을 때 스킬 발동
        if (Input.GetKeyDown(KeyCode.Alpha1) && !isCooldown)
        {
            UseSkill();
        }

        // 쿨타임 갱신
        if (isCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isCooldown = false;
                cooldownImage.fillAmount = 0f;  
            }
            else
            {
                cooldownImage.fillAmount = cooldownTimer / cooldownTime;
            }
        }
    }

    void UseSkill()
    {
        Debug.Log("스킬 1 발동!");
        StartCooldown();
    }

    void StartCooldown()
    {
        isCooldown = true;
        cooldownTimer = cooldownTime;
        cooldownImage.fillAmount = 1f;
    }
}
