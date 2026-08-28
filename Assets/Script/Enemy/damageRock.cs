using UnityEngine;

public class RockHitReceiver : MonoBehaviour
{
    private EnemyData enemyData;

    void Start()
    {
        // 같은 오브젝트에서 EnemyData 가져오기
        enemyData = GetComponent<EnemyData>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 오브젝트가 RockProjectile인지 확인
        RockProjectile rock = collision.GetComponent<RockProjectile>();
        if (rock != null && enemyData != null)
        {
            // 데미지 적용
            enemyData.enemy_current_HP -= rock.damage;

            // Destroy 제거 → 바위가 사라지지 않음
            // Destroy(rock.gameObject);
        }
    }
}
