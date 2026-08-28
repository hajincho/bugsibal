using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Skill5 : MonoBehaviour
{
    Animator animator;
    PlayerMove playermove;

    public float skill5_cooldown =10.0f;
    bool skill5_able = true;
    float timer = 0.0f;

    public Transform playertransform;
    Player player;
    public GameObject swordeffect;
    Vector3 newtrans;

    public Image skillcool;
    CoolDown cooldown2;

    void Start()
    {
        cooldown2 = skillcool.GetComponent<CoolDown>();
        animator = GetComponent<Animator>();
        playertransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && skill5_able == true)
        {
            cooldown2.UseSkill();
            skill5_able = false;
            Instantiate(swordeffect, playertransform.position  + new Vector3(0, 1, 0), Quaternion.identity);
            Invoke("SkillCoolDown", skill5_cooldown);

        }
    }

    void SkillCoolDown()
    {
        skill5_able = true;
    }

    void NotUsingSkills()
    {
        animator.SetInteger("Attack", -1);
    }

    public void SkillDamageEnable(IsSkillDamaged skilldamaged)
    {
        StartCoroutine(Skill(skilldamaged, skill5_cooldown, 5));
    }
    IEnumerator Skill(IsSkillDamaged skilldamaged, float delay, int skillnum)
    {
        yield return new WaitForSeconds(delay);
        skilldamaged.SkillAble(skillnum);
    }

}
