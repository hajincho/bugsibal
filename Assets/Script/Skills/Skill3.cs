using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill3 : MonoBehaviour
{
    [Header("Skill Settings")]
    public float skillRange = 6f;       // 스킬 반경 6m
    public int skillDamage = 35;        // 고정 데미지 35
    public float healRatio = 0.6f;      // 입힌 피해의 60% 회복
    public float cooldown = 8f;         // 쿨타임 8초
    public KeyCode useKey = KeyCode.Alpha2;  // 발동키 숫자 2

    [Header("VFX Settings")]
    public GameObject effectPrefab;     // 스킬 이펙트 프리팹

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.red;

    private bool skillOnCooldown = false;
    private float cooldownTimer = 0f;

    private Player player;
    private HashSet<EnemyData> processedEnemies = new HashSet<EnemyData>();

    void Start()
    {
        player = GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogWarning("[Skill3] Player 컴포넌트를 부모에서 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        if (skillOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                skillOnCooldown = false;
                cooldownTimer = 0f;
            }
        }

        if (Input.GetKeyDown(useKey) && !skillOnCooldown)
        {
            if (player != null && player.skill_using) return;

            StartCoroutine(UseSkill());
        }
    }

    IEnumerator UseSkill()
    {
        skillOnCooldown = true;
        cooldownTimer = cooldown;

        if (player != null)
            player.skill_using = true;

        // 스킬 이펙트 생성
        GameObject currentEffect = null;
        if (effectPrefab != null)
        {
            currentEffect = Instantiate(effectPrefab, transform.position, Quaternion.identity);
        }

        processedEnemies.Clear();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange);

        float totalDamageDealt = 0;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.tag != "Enemy") continue;

            EnemyData enemy = hit.GetComponent<EnemyData>() ?? hit.GetComponentInParent<EnemyData>();
            if (enemy == null) continue;
            if (processedEnemies.Contains(enemy)) continue;
            processedEnemies.Add(enemy);

            if (enemy.enemy_current_HP <= 0) continue;

            float beforeHP = enemy.enemy_current_HP;

            // 데미지 입히기
            enemy.enemy_current_HP = Mathf.Max(0, enemy.enemy_current_HP - skillDamage);

            float afterHP = enemy.enemy_current_HP;
            float applied = Mathf.Max(0, beforeHP - afterHP);
            totalDamageDealt += applied;

            enemy.Hitted();
        }

        // 회복량 계산 및 적용
        int healAmount = Mathf.RoundToInt(totalDamageDealt * healRatio);
        if (healAmount > 0 && player != null)
        {
            player.Heal(healAmount);
        }

        // 이펙트가 내 주위를 5바퀴 빠르게 돌게 하기 (지속시간 0.6초 유지)
        float duration = 0.6f;
        float elapsed = 0f;
        float rotations = 5f;
        float angleSpeed = 360f * rotations / duration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (currentEffect != null)
            {
                float angle = angleSpeed * elapsed;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * skillRange;
                currentEffect.transform.position = transform.position + offset;

                // 이펙트 자체 회전 (선택사항)
                currentEffect.transform.Rotate(Vector3.forward, 540f * Time.deltaTime);
            }
            yield return null;
        }

        if (currentEffect != null)
            Destroy(currentEffect);

        if (player != null)
            player.skill_using = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}