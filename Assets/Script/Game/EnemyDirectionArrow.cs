using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 5초 동안 적에게 피해를 주지 못하면 플레이어를 중심으로 한 원 위에 가장 가까운 적 방향 화살표를 표시한다.
// 목표는 10초 동안 고정해 거리가 바뀌어도 화살표가 흔들리지 않게 하고, 고정한 적이 사라지면
// 가깝고 주변에 적이 많은 쪽을 대신 가리킨다. 씬 설정 없이 AfterSceneLoad에서 스스로 생성된다.
public sealed class EnemyDirectionArrow : MonoBehaviour
{
    const float IdleSeconds = 5f, LockSeconds = 10f, ClusterRadius = 5f, ScreenRadius = 180f, ScanInterval = 0.25f, OrbitSpeed = 540f;
    // CameraMove가 카메라를 플레이어 y+1에 두므로, 몸 중심(=화면 중앙)을 원의 중심으로 쓴다.
    static readonly Vector3 BodyCenter = new Vector3(0f, 1f, 0f);

    static EnemyDirectionArrow instance;

    readonly List<EnemyData> enemies = new List<EnemyData>();
    Canvas canvas;
    RectTransform arrow;
    Transform player;
    EnemyData target;
    float lockedUntil, idleSince, nextScan, angle;
    bool shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("EnemyDirectionArrow");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<EnemyDirectionArrow>();
    }

    void Awake()
    {
        canvas = FeatureUI.Canvas("Canvas", transform, 5);
        arrow = FeatureUI.Image("Arrow", canvas.transform, new Vector2(36, 72), Vector2.zero, new Color(1f, .85f, .2f, .55f), ArrowSprite()).rectTransform;
        arrow.gameObject.SetActive(false);
        idleSince = Time.time;
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        idleSince = Time.time;
        target = null;
        player = null;
    }

    void Update()
    {
        if (Time.time >= nextScan) { nextScan = Time.time + ScanInterval; Scan(); }

        var cam = Camera.main;
        if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) player = p.transform; }
        bool idle = Time.time - Mathf.Max(idleSince, EnemyData.LastHitTime) >= IdleSeconds;
        if (!idle) target = null;
        if (!idle || enemies.Count == 0 || player == null || cam == null || StageRewardController.InputBlocked || GameManager.player_current_HP <= 0f)
        {
            shown = false;
            arrow.gameObject.SetActive(false);
            return;
        }

        Retarget();
        Vector2 dir = target == null ? Vector2.zero : (Vector2)(target.transform.position - (player.position + BodyCenter));
        if (dir.sqrMagnitude < 1e-4f) { shown = false; arrow.gameObject.SetActive(false); return; }

        // 목표 각도로 원 둘레를 따라 움직인다. 화살표는 항상 원 바깥쪽(= 자기 위치의 방향)을 가리킨다.
        float goal = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        angle = shown ? Mathf.MoveTowardsAngle(angle, goal, OrbitSpeed * Time.deltaTime) : goal;
        shown = true;

        var radial = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 screen = cam.WorldToScreenPoint(player.position + BodyCenter) + (Vector3)(radial * ScreenRadius * canvas.scaleFactor);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out var local);
        arrow.anchoredPosition = local;
        arrow.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        arrow.gameObject.SetActive(true);
    }

    void Scan()
    {
        enemies.Clear();
        foreach (var go in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            var data = go.GetComponent<EnemyData>();
            if (data != null && data.enemy_current_HP > 0f && data.GetComponent<TrainingDummy>() == null) enemies.Add(data);
        }
        if (enemies.Count == 0) idleSince = Time.time;
    }

    void Retarget()
    {
        bool alive = target != null && target.isActiveAndEnabled && target.enemy_current_HP > 0f;
        if (alive && Time.time < lockedUntil) return;

        // 잠금 중 대상이 사라졌으면(= 고정 방식이 깨졌으면) 가까우면서 적이 많이 모인 쪽, 잠금이 끝났으면 가장 가까운 적.
        bool lost = target != null && !alive && Time.time < lockedUntil;
        target = lost ? BestCluster() : Nearest();
        lockedUntil = Time.time + LockSeconds;
    }

    EnemyData Nearest()
    {
        EnemyData best = null;
        float bestDistance = float.MaxValue;
        foreach (var e in enemies)
        {
            float d = ((Vector2)(e.transform.position - player.position)).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = e; }
        }
        return best;
    }

    // 점수 = (주변 적 수 + 자신) / 거리. 많고 가까울수록 높다.
    EnemyData BestCluster()
    {
        EnemyData best = null;
        float bestScore = -1f;
        foreach (var e in enemies)
        {
            int crowd = 1;
            foreach (var other in enemies)
                if (other != e && ((Vector2)(other.transform.position - e.transform.position)).sqrMagnitude <= ClusterRadius * ClusterRadius) crowd++;
            float score = crowd / Mathf.Max(1f, Vector2.Distance(e.transform.position, player.position));
            if (score > bestScore) { bestScore = score; best = e; }
        }
        return best;
    }

    // 위쪽을 가리키는 화살표(몸통 + 삼각 머리). 머리와 꼬리가 구분되도록 세로로 길게 만든다. 별도 스프라이트 에셋 없이 생성한다.
    static Sprite ArrowSprite()
    {
        const int width = 32, height = 64, headStart = 28;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < height; y++)
        {
            float half = y < headStart ? width * .15f : (1f - (y - headStart) / (float)(height - headStart)) * width / 2f;
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, Mathf.Abs(x - width / 2f + .5f) <= half ? Color.white : Color.clear);
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f));
    }
}
