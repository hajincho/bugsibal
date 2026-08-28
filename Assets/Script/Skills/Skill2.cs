using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PokpalSkill : MonoBehaviour
{
    public float skill_cooldown = 9.0f;
    public Transform playertransform;
    Player player;
    public bool skill_ready = true;
    public GameObject pokpal;

    public Image skillcool;
    CoolDown cooldown3;


    void Awake()
    {
        cooldown3 = skillcool.GetComponent<CoolDown>();
        playertransform = GameObject.FindWithTag("Player").GetComponent<Transform>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && skill_ready == true)
        {
            cooldown3.UseSkill();
            skill_ready = false;

            StartCoroutine(Pokpal(playertransform.position));
            Invoke("SkillCoolDown", 3.0f);
        }
    }
    public void Spawn()
    {
        Instantiate(pokpal, new Vector2(playertransform.position.x - 5, playertransform.position.y), Quaternion.identity);
        Instantiate(pokpal, new Vector2(playertransform.position.x + 5, playertransform.position.y), Quaternion.identity);
        Instantiate(pokpal, new Vector2(playertransform.position.x, playertransform.position.y - 5), Quaternion.identity);
        Instantiate(pokpal, new Vector2(playertransform.position.x, playertransform.position.y + 5), Quaternion.identity);
    }
    void SkillCoolDown()
    {
        skill_ready = true;
    }

    IEnumerator Pokpal(Vector3 pos)
    {
        yield return new WaitForSeconds(0.4f);
        for (int i = 0; i < 4; i++)
        {
            Instantiate(pokpal, new Vector2(pos.x + 3 * i, pos.y), Quaternion.identity);
            Instantiate(pokpal, new Vector2(pos.x - 3 * i, pos.y), Quaternion.identity);
            Instantiate(pokpal, new Vector2(pos.x, pos.y - 3 * i), Quaternion.identity);
            Instantiate(pokpal, new Vector2(pos.x,pos.y + 3 * i), Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
        }
    }
    public void SkillDamageEnable(IsSkillDamaged skilldamaged)
    {
        StartCoroutine(Skill(skilldamaged, skill_cooldown, 2));
    }
    IEnumerator Skill(IsSkillDamaged skilldamaged, float delay, int skillnum)
    {
        yield return new WaitForSeconds(delay);
        skilldamaged.SkillAble(skillnum);
    }
}
