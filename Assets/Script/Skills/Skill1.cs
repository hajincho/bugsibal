using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Skill1 : MonoBehaviour
{
    Animator animator;

    public float skill1_cooldown = 7f;
    public int skill1_damage = 10;
    float angle;
    Vector2 target;
    public GameObject fire;
    public GameObject fireball;
    public Transform playertransform;

    Player player;
    public FB fb;
    FireEffect fireEffect;

    public bool skill1_able = true;
    public bool skill_ready = false;

    public Image skillcool;
    CoolDown cooldown1;

    void Awake()
    {
        cooldown1 = skillcool.GetComponent<CoolDown>();
        animator = GetComponent<Animator>();
        fb = fireball.GetComponent<FB>();
        playertransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    void Update()
    {

        if (Input.GetKey(KeyCode.Q) && playertransform != null && fb != null && skill1_able == true)
        {
            cooldown1.UseSkill();
            skill1_able = false;
            player.skill_using = true;
            animator.SetInteger("Attack", 3);
            player.FlipTowardMoveInput();
            Invoke("SpawnFireBall", 0.4f);
            Invoke("NotUsingSkills", 0.68f);
            Invoke("SkillCoolDown", 3.0f);

        }

    }
    void SpawnFire()
    {
        Vector3 spawnPosition = playertransform.position;
        Instantiate(fire, spawnPosition, Quaternion.identity);
    }

    public void SpawnFireBall()
    {
        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;
        Vector3 direction = (mouse - playertransform.position).normalized;
        angle = Mathf.Atan2(mouse.y - playertransform.position.y, mouse.x - playertransform.position.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);


        for (int i = 0; i < 3; i++)
        {
            GameObject fireballInstance = Instantiate(fireball, playertransform.position, rotation);
            FB fb_script = fireballInstance.GetComponent<FB>();
            fb_script.fbnum = i;
            fb_script.FireBall(direction);
        }
    }
    void SkillCoolDown()
    {
        skill1_able = true;
    }

    void NotUsingSkills()
    {
        animator.SetInteger("Attack", -1);
        player.skill_using = false;
    }

    public void SkillDamageEnable(IsSkillDamaged skilldamaged)
    {
        StartCoroutine(Skill(skilldamaged, skill1_cooldown, 1));
    }
    IEnumerator Skill(IsSkillDamaged skilldamaged, float delay, int skillnum)
    {
        yield return new WaitForSeconds(delay);
        skilldamaged.SkillAble(skillnum);
    }

}
