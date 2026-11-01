using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    //소환할 적과 위치를 세트로 받음
    //원하는 적과 위치를 순서를 맞춰서 배치
    public List<GameObject> enemies_phase1 = new List<GameObject>();
    public List<Transform> phase1_location = new List<Transform>();
    public List<GameObject> enemies_phase2 = new List<GameObject>();
    public List<Transform> phase2_location = new List<Transform>();
    public List<GameObject> enemies_phase3 = new List<GameObject>();
    public List<Transform> phase3_location = new List<Transform>();
    public List<GameObject> enemies_phase4 = new List<GameObject>();
    public List<Transform> phase4_location = new List<Transform>();
    // Optional route checkpoints; empty preserves the existing stage behavior.
    public List<Vector2> encounterCenters = new List<Vector2>();
    public float encounterActivationRadius = 12f;
    public bool autoStartThirdWave;
    [Min(0f)] public float thirdWaveDelay = 1f;
    //소환된 적의 개수를 세서 다음 phase로 넘어가는 용도
    public List<GameObject> summoned_enemies = new List<GameObject>();
    public GameObject next_scene;
    public Transform portal_position;
    public int phase;
    public bool progress = true;
    public GameObject denger;
    float timer;

    // Start is called before the first frame update
    void Start()
    {
        phase = 0;
    }

    // Update is called once per frame
    void Update()
    {
        summoned_enemies.RemoveAll(item => item == null);
        if (autoStartThirdWave && phase == 2)
            summoned_enemies.RemoveAll(item =>
            {
                var enemy = item.GetComponent<EnemyData>();
                return enemy != null && enemy.enemy_current_HP <= 0f;
            });
        if(summoned_enemies.Count == 0 && progress)
        {
            if (phase < encounterCenters.Count && !(autoStartThirdWave && phase == 2))
            {
                var player = FindObjectOfType<Player>();
                if (player == null || Vector2.Distance(player.transform.position, encounterCenters[phase]) > encounterActivationRadius) return;
            }
            switch(phase)
            {
                case 0:
                    progress = false;
                    for (int i = 0; i < enemies_phase1.Count; i++)//각 페이즈의 적 수만큼 반복
                    {
                        Instantiate(denger, phase1_location[i].position,transform.rotation);
                    }
                    Invoke("phase1", 3f);
                    return;
                case 1:
                    progress = false;
                    for (int i = 0; i < enemies_phase2.Count; i++)//각 페이즈의 적 수만큼 반복
                    {
                        Instantiate(denger, phase2_location[i].position, transform.rotation);
                    }
                    Invoke("phase2", 3f);
                    return;
                case 2:
                    progress = false;
                    for (int i = 0; i < enemies_phase3.Count; i++)//각 페이즈의 적 수만큼 반복
                    {
                        var warning = Instantiate(denger, phase3_location[i].position, transform.rotation);
                        if (autoStartThirdWave) Destroy(warning, Mathf.Max(0f, thirdWaveDelay));
                    }
                    Invoke("phase3", autoStartThirdWave ? Mathf.Max(0f, thirdWaveDelay) : 3f);
                    return;
                case 3:
                    if (enemies_phase4.Count > 0)
                    {
                        progress = false;
                        for (int i = 0; i < enemies_phase4.Count; i++)
                            Instantiate(denger, phase4_location[i].position, transform.rotation);
                        Invoke("phase4", 3f);
                        return;
                    }
                    FinishStage();
                    return;
                case 4:
                    FinishStage();
                    return;
            }
        }
    }
    void FinishStage()
    {
        progress = false;
        if (StageRewardController.Ensure() != null) StageRewardController.Instance.MarkCleared();
        Instantiate(next_scene, portal_position.position, transform.rotation);
        phase++;
    }
    void phase4()
    {
        for (int i = 0; i < enemies_phase4.Count; i++)
            summoned_enemies.Add(Instantiate(enemies_phase4[i], phase4_location[i].position, transform.rotation));
        phase++;
        progress = true;
    }
    void phase1()
    {
        for (int i = 0; i < enemies_phase1.Count; i++)//각 페이즈의 적 수만큼 반복
        {
            summoned_enemies.Add(Instantiate(enemies_phase1[i], phase1_location[i].position, transform.rotation));//적 소환
        }
        phase++;
        progress = true;
        return;
    }
    void phase2()
    {
        for (int i = 0; i < enemies_phase2.Count; i++)//각 페이즈의 적 수만큼 반복
        {
            summoned_enemies.Add(Instantiate(enemies_phase2[i], phase2_location[i].position, transform.rotation));//적 소환
        }
        phase++;
        progress = true;
        return;
    }
    void phase3()
    {
        for (int i = 0; i < enemies_phase3.Count; i++)//각 페이즈의 적 수만큼 반복
        {
            summoned_enemies.Add(Instantiate(enemies_phase3[i], phase3_location[i].position, transform.rotation));//적 소환
        }
        phase++;
        progress = true;
        return;
    }
}
