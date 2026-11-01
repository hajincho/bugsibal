using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyCounter : MonoBehaviour
{
    public Text enemyCountText; // UI Text 컴포넌트를 연결하세요.

    void Start()
    {
        UpdateEnemyCount(); // 게임 시작 시 초기 적의 수를 업데이트
    }

    void Update()
    {
        UpdateEnemyCount(); // 매 프레임마다 적의 수를 업데이트
    }

    // 적의 수를 업데이트하는 메서드
    void UpdateEnemyCount()
    {
        // 'enemy' 태그 중 체력이 남은 적만 셉니다. 죽는 연출 중인 적과 EnemyData 없는 투사체(ThrowSword)는 제외
        int enemyCount = 0;
        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            EnemyData data = enemy.GetComponent<EnemyData>();
            if (data != null && data.GetComponent<TrainingDummy>() == null && data.enemy_current_HP > 0) enemyCount++;
        }

        // 적의 수를 UI 텍스트에 표시합니다.
        enemyCountText.text = "남은 적의 수: " + enemyCount;
    }
}

