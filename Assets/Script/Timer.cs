using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Timer : MonoBehaviour
{
    static Timer instance;
    public TMP_Text speedruntimer;
    float timer = 0;
    bool running;

    void Awake()
    {
        // DontDestroyObject가 중복 오브젝트를 지우므로 처음 생긴 것만 기억
        if (instance == null) instance = this;
        if (speedruntimer == null) speedruntimer = GetComponent<TMP_Text>();
        Render();
    }

    public float ElapsedSeconds => timer;
    public bool IsRunning => running;
    void Start()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name.StartsWith("Stage") || scene.name == "MidBoss" || scene.name == "FinalBoss") running = true;
    }
    void OnDestroy() { if (instance == this) instance = null; }
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    // Stage1 진입 시 0부터 시작. 일시정지, 보상 선택, 게임오버는 timeScale 0이라 Time.deltaTime이 0이 되어 자동으로 멈춤
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single && scene.name == "Stage1")
        {
            timer = 0;
            running = true;
        }
    }

    public static void Stop()
    {
        if (instance != null) instance.running = false;
    }

    void Update()
    {
        if (running && GameManager.player_current_HP > 0f && !StageRewardController.InputBlocked) timer += Time.deltaTime;
        Render();
    }
    void Render()
    {
        if (speedruntimer == null) return;
        speedruntimer.text = string.Format("{0:00}:{1:00}.{2:00}", (int)(timer / 60), (int)(timer % 60), (int)(timer * 100 % 100));
    }
}
