using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossHP : MonoBehaviour
{
    public EnemyData boss_data;
    public Image HP;

    void Start()
    {
        StageRewardController.Ensure();
        if (HP != null && HP.transform.parent != null) HP.transform.parent.gameObject.SetActive(false);
    }

    void Update()
    {
        if (HP != null && boss_data != null && boss_data.enemy_HP > 0) HP.fillAmount = Mathf.Clamp01(boss_data.enemy_current_HP/boss_data.enemy_HP);
    }
}
