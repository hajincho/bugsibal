using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Ultimate : MonoBehaviour
{
    Animator animator;

    public float ult_cooldown = 45.0f;
    bool ult_able = true;
    float timer = 0.0f;

    public Transform playertransform;
    Player player;
    public GameObject slash;
    public GameObject shoot;
    public GameObject dropsword;
    Vector3 newtrans;

    public Image skillcool;
    CoolDown cooldownult;

    void Start()
    {
        cooldownult = skillcool.GetComponent<CoolDown>();
        animator = GetComponent<Animator>();
        playertransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.X) && ult_able == true)
        {
            cooldownult.UseSkill();
            Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition) - playertransform.position;
            mouse.z = 0f;
            Vector3 direction = mouse.normalized;

            StartCoroutine(Ultim(direction));
            ult_able = false;

            Invoke("SkillCoolDown", ult_cooldown);

        }
    }

    void SkillCoolDown()
    {
        ult_able = true;
    }

    void NotUsingSkills()
    {
        animator.SetInteger("Attack", -1);
    }

    public void SkillDamageEnable(IsSkillDamaged skilldamaged, int skillnum)
    {
        StartCoroutine(Skill(skilldamaged, ult_cooldown, skillnum));
    }
    IEnumerator Skill(IsSkillDamaged skilldamaged, float delay, int skillnum)
    {
        yield return new WaitForSeconds(delay);
        skilldamaged.SkillAble(skillnum);
    }

    IEnumerator Ultim(Vector3 dir)
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        float maxRange = 10f;
        Vector3 spawnPos;

        float distance = Vector3.Distance(playertransform.position, mousePos);

        if (distance <= maxRange)
        {
            spawnPos = mousePos;
        }
        else
        {
            spawnPos = playertransform.position + dir * maxRange;
        }

        Instantiate(slash, playertransform.position, Quaternion.identity);
        yield return new WaitForSeconds(0.5f);

        Instantiate(dropsword, spawnPos + new Vector3(0, 10, 0), Quaternion.identity);
        //yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < 3;  i++)
        {
            GameObject shooting = Instantiate(shoot, playertransform.position + dir * 2, Quaternion.identity);
            Shoot shootscript = shooting.GetComponent<Shoot>();
            shootscript.SetDirection(dir, i);
        }
    }



}
