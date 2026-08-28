using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Skill2 : MonoBehaviour
{
    [Header("Skill Settings")]
    public float skillRange = 6f;       // 반경 6
    public int skillDamage = 35;        // 고정 데미지 35
    public float healRatio = 0.6f;      // 입힌 피해의 60% 회복
    public float cooldown = 8f;         // 쿨타임 8초
    public KeyCode useKey = KeyCode.Alpha2;  // 숫자 2키로 발동

    [Header("VFX / SFX (Optional)")]
    public GameObject vfxPrefab;        // 스킬 VFX(옵션)
    public AudioClip sfxClip;           // 스킬 SFX(옵션)

    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.red;

    // 내부 상태
    Player player;
    private bool skillOnCooldown = false;
    private float cooldownTimer = 0f;
    private HashSet<EnemyData> processedEnemies = new HashSet<EnemyData>();

    void Start()
    {
        player = GetComponentInParent<Player>();
        if (player == null)
            Debug.LogWarning("[Skill2] Player component not found in parent.");
    }

    void Update()
    {
        // 쿨타임 진행 (UI 없음)
        if (skillOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                skillOnCooldown = false;
                cooldownTimer = 0f;
            }
        }

        // 입력 처리 (숫자 2키)
        if (Input.GetKeyDown(useKey) && !skillOnCooldown)
        {
            // Player 측 재진입 플래그 확인 (있으면 true로 설정)
            if (player != null && player.skill_using) return;

            StartCoroutine(UseSkill());
        }
    }

    IEnumerator UseSkill()
    {
        // 재진입 차단 및 쿨다운 시작
        skillOnCooldown = true;
        cooldownTimer = cooldown;

        if (player != null) player.skill_using = true;

        // VFX / SFX (선택)
        if (vfxPrefab != null)
            Instantiate(vfxPrefab, transform.position, Quaternion.identity);

        if (sfxClip != null)
            AudioSource.PlayClipAtPoint(sfxClip, transform.position);

        // 대상 검색 (레이어 대신 태그 "Enemy"만 사용)
        processedEnemies.Clear();
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillRange);

        float totalDamageDealt = 0;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hit.tag != "Enemy") continue; // 태그로 필터

            EnemyData enemy = hit.GetComponent<EnemyData>() ?? hit.GetComponentInParent<EnemyData>();
            if (enemy == null) continue;
            if (processedEnemies.Contains(enemy)) continue; // 중복 콜라이더 방지
            processedEnemies.Add(enemy);

            if (enemy.enemy_current_HP <= 0) continue; // 이미 죽음

            float beforeHP = enemy.enemy_current_HP;

            // EnemyData에 TakeDamage가 있으면 호출, 없으면 직접 감소
            MethodInfo takeDamageMethod = enemy.GetType().GetMethod("TakeDamage", new System.Type[] { typeof(int) });
            if (takeDamageMethod != null)
            {
                takeDamageMethod.Invoke(enemy, new object[] { skillDamage });
            }
            else
            {
                enemy.enemy_current_HP = Mathf.Max(0, enemy.enemy_current_HP - skillDamage);
            }

            float afterHP = enemy.enemy_current_HP;
            float applied = Mathf.Max(0, beforeHP - afterHP);
            totalDamageDealt += applied;

            // 피격 반응 호출(Hitted가 있다면)
            MethodInfo hittedMethod = enemy.GetType().GetMethod("Hitted", System.Type.EmptyTypes);
            if (hittedMethod != null)
                hittedMethod.Invoke(enemy, null);
        }

        // 회복 적용(총 입힌 피해의 healRatio)
        int healAmount = Mathf.RoundToInt(totalDamageDealt * healRatio);
        if (healAmount > 0)
        {
            if (player != null)
                player.Heal(healAmount); // Player 쪽 Heal 사용
            else
                GameManager.player_current_HP = Mathf.Min(GameManager.player_current_HP + healAmount, GameManager.player_HP);
        }

        // 한 프레임 대기 후 플래그 해제
        yield return null;

        if (player != null) player.skill_using = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, skillRange);
    }
}