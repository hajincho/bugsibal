using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SwordMan : MonoBehaviour
{
    Animator animator;
    Rigidbody2D rigid;
    EnemyData enemy_data;
    EnemyMove enemy_move;

    public enum AttackMode
    {
        None = -1,
        ParryWindow = -2,
        Normal = 0,
        Down = 1,
        Sting = 2,
        Throw = 3
    }
    public AttackMode attack_mode = AttackMode.None;

    public bool attack_able = true;
    public bool ischarging = false;

    public GameObject boom;
    public GameObject sword;
    public Transform boom_position;

    public GameObject charging;
    public GameObject red_effect;
    public GameObject blue_effect;
    public bool on_red = false;
    float blue_HP;
    float lastHP;

    GameObject player;
    Player player_script;

    public List<float> cooltimes = new List<float>();
    public bool able0 = true;
    public bool able1 = true;
    public bool able2 = true;
    public bool able3 = true;
    public bool able4 = true;

    bool dead = false;

    GameObject wall;
    Collider2D wallCollider;

    void Start()
    {
        animator = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        enemy_data = GetComponent<EnemyData>();
        enemy_move = GetComponent<EnemyMove>();

        player = GameObject.FindWithTag("Player");
        if (player != null)
            player_script = player.GetComponent<Player>();

        wall = GameObject.Find("wall");
        if (wall != null)
            wallCollider = wall.GetComponent<Collider2D>();
        Invoke("Able4()", 5f);

        lastHP = enemy_data.enemy_current_HP;
    }

    void Update()
    {
        if (dead || player == null || player_script == null) return;

        float distance = Vector2.Distance(player.transform.position, transform.position);

        if (distance < 4)
        {
            if (able0 && attack_able)
                StartCoroutine(Attack());

            if (able2 && attack_able)
                StartCoroutine(StingAndParrying());

            if (able3 && attack_able)
                StartCoroutine(Charge());
        }
        else if (PlayerBelow())
        {
            if (able1 && attack_able)
                StartCoroutine(AttackDown());
        }
        else if (10< distance)
        {
            if (able4 && attack_able)
                StartCoroutine(SwordThrow());
        }

        DetectAndHandleStuck();

        if (enemy_data.enemy_current_HP <= 0 && !dead)
        {
            dead = true;
            animator.SetTrigger("Neutralize");
            rigid.velocity = Vector3.zero;
            attack_able = false;
            enemy_move.moveable = false;
            Destroy(gameObject, 2f);
        }

        if (ischarging)
        {
            float currentHP = enemy_data.enemy_current_HP;

            if (currentHP < lastHP)
            {
                float damage = lastHP - currentHP;
                float heal = damage * 0.5f;

                enemy_data.enemy_current_HP += heal;

                enemy_data.enemy_current_HP = Mathf.Min(enemy_data.enemy_current_HP, enemy_data.enemy_HP);
            }

            lastHP = enemy_data.enemy_current_HP;
        }
        else
        {
            lastHP = enemy_data.enemy_current_HP;
        }
    }

    bool PlayerBelow()
    {
        bool yInRange = player.transform.position.y < transform.position.y - 3.5f && player.transform.position.y >= transform.position.y - 6.0f;

        bool xInRange = player.transform.position.x < transform.position.x + 3.0f && player.transform.position.x > transform.position.x - 3.0f;

        if (yInRange && xInRange)
        {
            return true;
        }

        return false;
    }

    IEnumerator Attack()
    {
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = false;
        able0 = false;
        attack_able = false;
        animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.5f);
        attack_mode = AttackMode.Normal;
        yield return new WaitForSeconds(0.333f);
        attack_mode = AttackMode.None;

        yield return new WaitForSeconds(0.5f);
        attack_able = true;
        enemy_move.moveable = true;

        float waitTime = Mathf.Max(0, GetCooltime(0) - 1.333f);
        yield return new WaitForSeconds(waitTime);
        able0 = true;
    }

    IEnumerator AttackDown()
    {
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = false;
        able1 = false;
        attack_able = false;
        animator.SetTrigger("AttackDown");

        yield return new WaitForSeconds(1.167f);
        attack_mode = AttackMode.Down;
        yield return new WaitForSeconds(0.333f);
        attack_mode = AttackMode.None;

        Instantiate(boom, boom_position.position, Quaternion.identity);

        yield return new WaitForSeconds(0.5f);
        attack_able = true;
        enemy_move.moveable = true;

        float waitTime = Mathf.Max(0, GetCooltime(1) - 2f);
        yield return new WaitForSeconds(waitTime);
        able1 = true;
    }

    IEnumerator StingAndParrying()
    {
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = false;
        able2 = false;
        attack_able = false;
        animator.SetTrigger("StingAndParrying");

        yield return new WaitForSeconds(0.5f);
        attack_mode = AttackMode.Sting;
        yield return new WaitForSeconds(2.5f);
        attack_mode = AttackMode.None;

        yield return new WaitForSeconds(0.333f);
        attack_mode = AttackMode.ParryWindow;
        yield return new WaitForSeconds(0.333f);
        attack_mode = AttackMode.None;

        yield return new WaitForSeconds(0.5f);
        attack_able = true;
        enemy_move.moveable = true;

        float waitTime = Mathf.Max(0, GetCooltime(2) - 4.167f);
        yield return new WaitForSeconds(waitTime);
        able2 = true;
    }

    public void Parrying()
    {
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = false;
        attack_able = false;
        animator.SetTrigger("Neutralize");
        Invoke("ToIdle", 5f);
    }

    IEnumerator Charge()
    {
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = false;
        able3 = false;
        attack_able = false;
        ischarging = true;
        animator.SetTrigger("Charge");

        Instantiate(charging, transform.position + Vector3.down * 4f, Quaternion.identity);
        blue_HP = enemy_data.enemy_current_HP;
        yield return new WaitForSeconds(3f);
        ischarging = false;
        if (enemy_data.enemy_current_HP < blue_HP - 150)
            Parrying();
        else
        {
            enemy_move.moveable = true;
            attack_able = true;
            StartCoroutine(Dash());
        }
        float waitTime = Mathf.Max(0, GetCooltime(3) - 3f);
        yield return new WaitForSeconds(waitTime);
        able3 = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (on_red && other.CompareTag("PlayerWeapon") && player_script.on_attack)
            StartCoroutine(Dash());
    }

    IEnumerator SwordThrow()
    {
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = false;
        able4 = false;
        attack_able = false;
        animator.SetTrigger("Neutralize");

        yield return new WaitForSeconds(0.8f);

        int swordCount = 8;
        float radius = 5f;
        float spawnDelay = 0.3f;

        for (int i = 0; i < swordCount; i++)
        {
            float angle = (360f / swordCount) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Vector3 spawnPos = transform.position + offset;

            Vector3 startPos = spawnPos + new Vector3(0, 1.5f, 0);

            GameObject swordInstance = Instantiate(sword, startPos, Quaternion.identity);

            StartCoroutine(MoveDownToPosition(swordInstance.transform, spawnPos, 0.25f));

            yield return new WaitForSeconds(0.25f);

            if (swordInstance != null)
            {
                Vector3 directionToPlayer = (player.transform.position - swordInstance.transform.position).normalized;
                ThrownSword swordScript = swordInstance.GetComponent<ThrownSword>();

                if (swordScript != null)
                    swordScript.Initialize(directionToPlayer);
            }

            yield return new WaitForSeconds(spawnDelay);
        }

        yield return new WaitForSeconds(2f);

        rigid.velocity = Vector3.zero;
        enemy_move.moveable = true;
        attack_able = true;

        yield return new WaitForSeconds(10f);
        able4 = true;
    }

    IEnumerator Dash()
    {
        Debug.Log("반격");
        on_red = false;
        red_effect.SetActive(false);
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = false;
        attack_able = false;
        animator.SetTrigger("Dash");
        yield return new WaitForSeconds(0.2f);

        Collider2D[] childColliders = GetComponentsInChildren<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        foreach (Collider2D collider in childColliders)
        {
            Physics2D.IgnoreCollision(playerCollider, collider, true);
            if (wallCollider != null)
                Physics2D.IgnoreCollision(wallCollider, collider, true);
        }

        Vector2 startPos = transform.position;
        Vector2 targetPos = player.transform.position;
        yield return StartCoroutine(MoveToPosition(targetPos, 0.133f));

        Vector2 overshoot = targetPos + (targetPos - startPos).normalized * 20f;
        yield return StartCoroutine(MoveToPosition(overshoot, 0.5f));
        yield return new WaitForSeconds(0.167f);

        yield return StartCoroutine(MoveToPosition(startPos, 0.5f));
        rigid.velocity = Vector2.zero;

        yield return new WaitForSeconds(0.2f);
        GameManager.player_current_HP -= GameManager.player_current_HP * 0.5f;
        player_script.Hited();
        yield return new WaitForSeconds(0.8f);

        foreach (Collider2D collider in childColliders)
        {
            Physics2D.IgnoreCollision(playerCollider, collider, false);
            if (wallCollider != null)
                Physics2D.IgnoreCollision(wallCollider, collider, false);
        }

        attack_able = true;
        enemy_move.moveable = true;
    }

    IEnumerator MoveToPosition(Vector2 destination, float timeToMove)
    {
        Vector2 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < timeToMove)
        {
            transform.position = Vector2.Lerp(startPos, destination, elapsedTime / timeToMove);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = destination;
    }

    IEnumerator MoveDownToPosition(Transform obj, Vector3 targetPos, float duration)
    {
        Vector3 startPos = obj.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            obj.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.position = targetPos;
    }

    void ToIdle()
    {
        rigid.velocity = Vector3.zero;
        enemy_move.moveable = true;
        attack_able = true;
    }

    float GetCooltime(int index)
    {
        return (index >= 0 && index < cooltimes.Count) ? cooltimes[index] : 1f;
    }

    void Able4()
    {
        able4 = true;
    }

    void DetectAndHandleStuck()
    {
        if (player == null) return;

        Collider2D bossCollider = GetComponentInChildren<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        if (!bossCollider.bounds.Intersects(playerCollider.bounds))
            return;

        Collider2D[] walls = GameObject.FindGameObjectsWithTag("Wall")
                                        .Select(w => w.GetComponent<Collider2D>())
                                        .Where(c => c != null)
                                        .ToArray();

        foreach (var wallCol in walls)
        {
            if (wallCol.IsTouching(playerCollider))
            {

                Vector3 direction = (player.transform.position.x < transform.position.x) ? Vector3.left : Vector3.right;
                player.transform.position = transform.position + direction * 3f;

                if (able2 && attack_able)
                {
                    StopAllCoroutines();
                    StartCoroutine(StingAndParrying());
                }
                return;
            }
        }
    }
}
