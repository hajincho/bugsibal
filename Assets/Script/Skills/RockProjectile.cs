using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class RockProjectile : MonoBehaviour
{
    [Header("Timing")]
    public float riseHeight = 1.5f;   // 상승 높이
    public float riseTime = 0.6f;     // 상승 시간
    public float stayTime = 0.4f;     // 최고점 머무는 시간
    public float fallTime = 0.6f;     // 하강 시간

    [Header("Damage")]
    public int damage = 10;           // 적에게 주는 피해량

    private Collider2D hitbox;
    private bool canDamage = false;   // 상승 중 공격 가능 여부
    private Vector3 startPos, endPos;

    void Awake()
    {
        hitbox = GetComponent<Collider2D>();
        if (hitbox != null)
        {
            hitbox.isTrigger = true;
            hitbox.enabled = false;
        }
    }

    // 바위를 특정 방향으로 솟구치게 시작
    public void Activate(Vector3 riseDirection)
    {
        startPos = transform.position;
        endPos = startPos + riseDirection.normalized * riseHeight;

        // Animator 속도 자동 조정
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            float originalRiseTime = 0.6f; // 기본 riseTime
            anim.speed = originalRiseTime / riseTime;
        }

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // --- 상승 ---
        canDamage = true;
        if (hitbox != null) hitbox.enabled = true;

        float t = 0f;
        while (t < riseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / riseTime);
            transform.position = Vector3.Lerp(startPos, endPos, k);
            yield return null;
        }
        transform.position = endPos;

        // --- 최고점 머무르기 ---
        canDamage = false;
        if (hitbox != null) hitbox.enabled = false;
        if (stayTime > 0f) yield return new WaitForSeconds(stayTime);

        // --- 하강 ---
        t = 0f;
        while (t < fallTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fallTime);
            transform.position = Vector3.Lerp(endPos, startPos, k);
            yield return null;
        }
        transform.position = startPos;

        // --- 제거 ---
        Destroy(gameObject);
    }

    // OnTriggerEnter2D는 한 번만 존재
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canDamage) return;

        EnemyData enemyData = other.GetComponent<EnemyData>();
        if (enemyData != null)
        {
            // 기존 damage + GameManager.player_skill_power 합산
            int totalDamage = damage + GameManager.player_skill_power;
            enemyData.enemy_current_HP -= totalDamage;
            enemyData.Hitted();
        }
    }
}
