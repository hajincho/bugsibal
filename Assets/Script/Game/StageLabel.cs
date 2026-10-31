using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 미니맵 아래에 현재 맵 번호와 다음 보스까지 남은 맵 수를 표시.
// Build Settings 순서대로 "Stage<숫자>" 또는 이름에 "Boss"가 든 씬에 1부터 번호를 매기므로, 새 맵은 이름만 맞춰 순서에 넣으면 된다.
public class StageLabel : MonoBehaviour
{
    TMP_Text label;

    void Awake() { label = GetComponent<TMP_Text>(); }
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; Refresh(SceneManager.GetActiveScene()); }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) Refresh(scene); }

    static string NameAt(int buildIndex) => Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex));
    static bool IsBoss(string name) => name.Contains("Boss");
    static bool IsMap(string name) => IsBoss(name) || Regex.IsMatch(name, @"^Stage\d+$");

    void Refresh(Scene scene)
    {
        int current = scene.buildIndex, number = 0, toBoss = -1;
        if (current < 0 || !IsMap(scene.name)) { label.text = ""; return; }

        for (int i = 0; i <= current; i++) if (IsMap(NameAt(i))) number++;
        bool bossAhead = false;
        for (int i = current + 1; i < SceneManager.sceneCountInBuildSettings && !bossAhead; i++)
            if (IsBoss(NameAt(i))) { bossAhead = true; toBoss = i - current; }

        string second = IsBoss(scene.name) ? (bossAhead ? "MID BOSS" : "FINAL BOSS")
                      : bossAhead ? "보스까지 " + toBoss + "개 맵" : "";
        label.text = "STAGE " + number + (second == "" ? "" : "\n" + second);
    }
}
