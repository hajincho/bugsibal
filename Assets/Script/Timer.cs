using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Timer : MonoBehaviour
{
    static Timer instance;
    public TMP_Text speedruntimer;
    float timer = 0;

    Vector2 originalAnchorMin, originalAnchorMax, originalPivot, originalPosition;
    Vector3 originalScale;
    TextAlignmentOptions originalAlignment;
    int originalSiblingIndex;
    bool layoutSaved;

    void SaveLayout()
    {
        if (layoutSaved || speedruntimer == null) return;
        RectTransform rect = speedruntimer.rectTransform;
        originalAnchorMin = rect.anchorMin;
        originalAnchorMax = rect.anchorMax;
        originalPivot = rect.pivot;
        originalPosition = rect.anchoredPosition;
        originalScale = rect.localScale;
        originalAlignment = speedruntimer.alignment;
        originalSiblingIndex = rect.GetSiblingIndex();
        layoutSaved = true;
    }

    void RestoreLayout()
    {
        if (!layoutSaved || speedruntimer == null) return;
        RectTransform rect = speedruntimer.rectTransform;
        rect.anchorMin = originalAnchorMin;
        rect.anchorMax = originalAnchorMax;
        rect.pivot = originalPivot;
        rect.anchoredPosition = originalPosition;
        rect.localScale = originalScale;
        rect.SetSiblingIndex(originalSiblingIndex);
        speedruntimer.alignment = originalAlignment;
    }

    void ShowFinalTime()
    {
        if (speedruntimer == null) return;
        SaveLayout();
        RectTransform rect = speedruntimer.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = originalScale * 3f;
        rect.SetAsLastSibling();
        speedruntimer.alignment = TextAlignmentOptions.Center;
    }
    bool running;

void Awake()
    {
        // DontDestroyObject가 중복 오브젝트를 지우므로 처음 생긴 것만 기억
        if (instance == null) instance = this;
        SaveLayout();
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    // Stage1 진입 시 0부터 시작. 일시정지, 보상 선택, 게임오버는 timeScale 0이라 Time.deltaTime이 0이 되어 자동으로 멈춤
void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && scene.name == "Stage1")
        {
            RestoreLayout();
            timer = 0;
            running = true;
            if (speedruntimer != null) speedruntimer.text = "00:00.00";
        }
    }

public static void Stop()
    {
        if (instance == null) return;
        instance.running = false;
        instance.ShowFinalTime();
    }

    void Update()
    {
        if (!running) return;
        timer += Time.deltaTime;
        speedruntimer.text = string.Format("{0:00}:{1:00}.{2:00}", (int)(timer / 60), (int)(timer % 60), (int)(timer * 100 % 100));
    }
}
