using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    [SerializeField] List<GameObject> weapons = new List<GameObject>();
    [SerializeField] List<GameObject> sword_effects = new List<GameObject>();
    [SerializeField] Animator bow_animator;

    Animator animator;
    GameManager gamemanager;
    ItemManager itemmanager;
    PlayerMove playermove;
    Transform playertransform;
    ItemDrop itemdrop;
    public GameObject trashsword;
    public GameObject hitted_image;

    // 대시 관련 변수
    Vector3 dash_direction;
    public bool on_dash = false;
    public bool dash_able = true;
    public float dash_cooltime = 7.5f;
    public float dash_duration = 0.5f;
    public float dash_time;
    public UnityEngine.UI.Image dash_cool_down;

    // 기타 상태
    public bool on_attack = false;
    public int weapon_mode; // 0:검 1:활 2:강화검
    public int sword_num; // 칼 아이템 번호
    public int bow_num; // 활 아이템 번호
    public GameObject sword_effect;
    public bool weakeningable = true;
    public bool midboss_cleared = false;
    public int medicine;

    //스킬 관련 변수
    public bool skill_using = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playertransform = GetComponent<Transform>();
        itemdrop = GetComponent<ItemDrop>();
        gamemanager = GameObject.FindGameObjectWithTag("Manager").GetComponent<GameManager>();
        itemmanager = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemManager>();
        playermove = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMove>();
        hitted_image.SetActive(false);
        dash_able = false;

        weapon_mode = 1;
        sword_num = 1;
        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].SetActive(false);
        }
        weapons[1].SetActive(true);


        animator.SetInteger("Attack", -1);
        animator.SetInteger("Attacked", -1);
    }

    void Update()
    {
        // 무기 전환
        if (Input.GetKeyDown(KeyCode.P))
        {
            switch (weapon_mode)
            {
                case 0:
                    weapon_mode = 1;
                    weapons[0].SetActive(false);
                    weapons[sword_num].SetActive(true);
                    gamemanager.AddItemValue();

                    break;
                case 1:
                    weapon_mode = 0;
                    for (int i = 0; i < weapons.Count; i++)
                    {
                        weapons[i].SetActive(false);
                    }
                    weapons[0].SetActive(true);
                    break;
            }
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            sword_num += 1;
            //if (weapon_mode == 1)
            //{
                //for (int i = 0; i < weapons.Count; i++)
                //{
                   // weapons[i].SetActive(false);
                //}
               // weapons[sword_num].SetActive(true);
               // gamemanager.AddItemValue();
            //}
           // Debug.Log(sword_num);
        }
        if (sword_num <= 0 || sword_num >= weapons.Count)
        {
            sword_num = 1;
        }
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log(sword_num + ":" + GameManager.player_HP);
            Debug.Log(sword_num + ":" +  GameManager.player_current_HP);
            Debug.Log(sword_num + ":" + GameManager.player_power);
            Debug.Log(sword_num + ":" + GameManager.player_skill_power);
        }


        // 공격 처리
        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift) & !on_attack && !on_dash && !skill_using)
        {
            on_attack = true;

            switch (weapon_mode)
            {
                case 1:
                    //sword_effects[sword_num -1].SetActive(true);
                    animator.SetInteger("Attack", 0);
                    if (playermove.player_direction == 1) 
                    {
                        StartCoroutine(InstEffect(1,0.2f ));
                    }
                    else if (playermove.player_direction == 0)
                    {
                        StartCoroutine(InstEffect(-1, 0.2f));
                    }
                    FlipTowardMoveInput();
                    Invoke(nameof(ToIdle), 0.68f);
                    break;

                case 0:
                    animator.SetInteger("Attack", 1);
                    bow_animator.enabled = true;
                    FlipTowardMoveInput();
                    Invoke(nameof(ToIdle), 0.833f);
                    break;
            }
        }

        // 대시 입력 (WASD 방향)
        if (Input.GetKeyDown(KeyCode.Space) && dash_able)
        {
            Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            if (inputDir == Vector2.zero) return; // 입력이 없으면 대시 안 함

            dash_direction = inputDir;

            dash_able = false;
            on_dash = true;
            dash_time = 0f;

            on_attack = false;
            PlayerMove.moveable = false;
            bow_animator.enabled = false;
            animator.SetTrigger("Dash");
            dash_cool_down.fillAmount = 1f;

            FlipTowardMoveInput();
            StartCoroutine(Dash());
        }

        if (Input.GetKeyDown(KeyCode.Tab)) //Q키로 아이템 줍기
        {
            SwordDrop nearest = FindNearestSwordDrop();
            if (nearest != null)
            {
                nearest.AddItem(nearest.sword);
                nearest.Pickup();
                GameObject swordtrash = Instantiate(trashsword ,playertransform.position,  Quaternion.identity);
                SwordDrop sworddrop = swordtrash.GetComponent<SwordDrop>();
                sworddrop.swordnum = sword_num - 1;
                foreach (Item item in itemmanager.itemList)
                {
                    if (item.value == sword_num)
                    {
                        sworddrop.sword = item;
                        break;
                    }
                }
                sworddrop.SwordChange(sword_num - 1);
                sword_num = nearest.swordnum + 1;
                gamemanager.AddItemValue();
                if (weapon_mode == 1)
                {
                    for (int i = 0; i < weapons.Count; i++)
                    {
                        weapons[i].SetActive(false);
                    }
                    weapons[sword_num].SetActive(true);
                }

            }
        }
    }
    SwordDrop FindNearestSwordDrop() //가장 가까운 드롭 아이템 찾기
    {
        SwordDrop[] drops = FindObjectsOfType<SwordDrop>();
        SwordDrop nearest = null;
        float minDist = Mathf.Infinity;

        foreach (SwordDrop drop in drops)
        {
            float dist = Vector3.Distance(transform.position, drop.transform.position);
            if (dist < drop.distance && dist < minDist)
            {
                minDist = dist;
                nearest = drop;
            }
        }
        return nearest;
    }

    public void FlipTowardMoveInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal > 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (horizontal < 0)
            transform.localScale = new Vector3(1, 1, 1);
    }

    void ToIdle()
    {
        animator.SetInteger("Attack", -1);
        on_attack = false;
        PlayerMove.moveable = true;
        //sword_effects[sword_num - 1].SetActive(false );

    }

    public void Attacked(int Attacked)
    {
        if (!on_dash)
        {
            animator.SetInteger("Attacked", Attacked);
            PlayerMove.moveable = false;
            Invoke(nameof(ReMove), Attacked == 0 ? 0.2f : 1f);
        }
    }

    void ReMove()
    {
        animator.SetInteger("Attacked", -1);
        PlayerMove.moveable = true;
    }

    IEnumerator Dash()
    {
        float dashElapsed = 0f;
        float dashSpeed = 20f;

        while (dashElapsed < dash_duration)
        {
            transform.position += dash_direction * dashSpeed * Time.deltaTime;
            dashElapsed += Time.deltaTime;
            yield return null;
        }

        on_dash = false;
        ReMove();

        while (dash_time < dash_cooltime)
        {
            dash_time += Time.deltaTime;
            dash_cool_down.fillAmount = 1f - (dash_time / dash_cooltime);
            yield return null;
        }

        dash_cool_down.fillAmount = 0f;
        dash_able = true;
    }

    public void Hited()
    {
        hitted_image.SetActive(true);
        Invoke(nameof(ToOriginColor), 0.3f);
    }

    void ToOriginColor()
    {
        hitted_image.SetActive(false);
    }

    public void WeaponUpgrade()
    {
        midboss_cleared = true;
        GameManager.player_power += 7;
        GameManager.player_HP += 50;
        GameManager.player_current_HP += 50;
        weapons[0].SetActive(false);

        switch (weapon_mode)
        {
            case 0:
                weapon_mode = 2;
                weapons[2].SetActive(true);
                weapons[1].SetActive(false);
                break;
            case 1:
                weapon_mode = 1;
                weapons[2].SetActive(false);
                weapons[1].SetActive(true);
                break;
        }
    }
    public void Heal(int amount)
    {
        GameManager.player_current_HP = Mathf.Min(GameManager.player_current_HP + amount, GameManager.player_HP);
    }

    



    IEnumerator InstEffect(int dir, float time)
    {
        yield return new WaitForSeconds(time);
        if (sword_num == 10)
        {
            Instantiate(sword_effects[sword_num - 1], playertransform.position + new Vector3(-2.8f * dir, 1.5f, 1), Quaternion.identity);
        }
        else
        {
            Instantiate(sword_effects[sword_num - 1], playertransform.position + new Vector3(-2.3f * dir, 1.5f, 1), Quaternion.identity);
        }

    }

    public void SkillDamageEnable(IsSkillDamaged skilldamaged)//적이 공격당했는지 여부 초기화
    {
        StartCoroutine(Skill(skilldamaged,0.4f, 0));
    }
    IEnumerator Skill(IsSkillDamaged skilldamaged, float delay, int skillnum)
    {
        yield return new WaitForSeconds(delay);
        skilldamaged.SkillAble(skillnum);
    }
}
