using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RockSkillController : MonoBehaviour
{
    [Header("Skill Settings")]
    public GameObject[] rocks;          // 소환할 바위 프리팹 배열
    public float[] rockSpacings;        // 각 바위 사이 간격
    public float initialSpawnOffset = 1.0f; // 첫 바위 위치 오프셋
    public float interval = 0.2f;       // 바위 생성 간격
    public float skillCooldown = 2f;    // 스킬 쿨타임
    public float sizeMultiplier = 1.0f; // 바위 크기 조절

    private PlayerMove playerMove;
    private bool canUse = true;

    public Image skillcool;
    CoolDown cooldown5;

    void Start()
    {
        cooldown5 = skillcool.GetComponent<CoolDown>();
        playerMove = GetComponent<PlayerMove>();
        if (playerMove == null)
            Debug.LogError("[RockSkillController] PlayerMove 컴포넌트를 찾을 수 없습니다!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C) && canUse)
        {
            cooldown5.UseSkill();
            StartCoroutine(RunSkill());


            
        }
    }

    private IEnumerator RunSkill()
    {
        canUse = false;

        float currentOffset = initialSpawnOffset;

        // 시전 시점 기준으로 위치와 방향 고정
        Vector3 startPos = transform.position;
        int startDirection = playerMove.player_direction;

        for (int i = 0; i < rocks.Length; i++)
        {
            if (rocks[i] == null)
            {
                Debug.LogWarning("[RockSkillController] rocks[" + i + "]가 비어 있음!");
                continue;
            }

            // 고정된 방향 사용
            Vector3 direction = (startDirection == 0) ? Vector3.right : Vector3.left;

            // 고정된 위치 기준으로 spawn
            Vector3 spawnPos = startPos + direction * currentOffset;

            GameObject rock = Instantiate(rocks[i], spawnPos, Quaternion.identity);

            // 바위 크기: sizeMultiplier의 1.5배 적용
            float finalScale = sizeMultiplier * 3.0f;

            if (startDirection == 1)
                rock.transform.localScale = new Vector3(-1, 1, 1) * finalScale;
            else
                rock.transform.localScale = Vector3.one * finalScale;

            // 바위가 위로 솟게
            RockProjectile rockScript = rock.GetComponent<RockProjectile>();
            if (rockScript != null)
                rockScript.Activate(Vector3.up);

            yield return new WaitForSeconds(interval);

            currentOffset += rockSpacings[i];
        }

        // 쿨타임
        yield return new WaitForSeconds(skillCooldown);
        canUse = true;
    }
}
