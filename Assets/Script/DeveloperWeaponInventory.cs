using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Wired only in DontDestroyObject; its UI follows the persistent player through stages.
[DefaultExecutionOrder(-500)]
public sealed class DeveloperWeaponInventory : MonoBehaviour
{
    public ItemManager itemManager;
    public Player player;
    public TMP_FontAsset font;
    public static DeveloperWeaponInventory Instance { get; private set; }
    public static bool InputBlocked => Instance != null &&
        (Instance.IsOpen || Time.frameCount <= Instance.blockThroughFrame);
    public bool IsOpen { get; private set; }
    public List<Item> AvailableWeapons { get; private set; } = new List<Item>();
    const int PageSize = 16;
    readonly List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();
    readonly List<UnityEngine.UI.Image> icons = new List<UnityEngine.UI.Image>();
    readonly List<TextMeshProUGUI> names = new List<TextMeshProUGUI>();
    GameObject panel;
    TextMeshProUGUI status, pageLabel;
    UnityEngine.UI.Button previous, next;
    int page, blockThroughFrame = -1;
    float priorTimeScale;
    CursorLockMode priorCursorLock;
    bool priorCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPanel();
        SceneManager.sceneLoaded += SceneLoaded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Slash)) SetOpen(!IsOpen);
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsOpen) SetOpen(false);
    }

    void OnDisable() { if (Instance == this && IsOpen) SetOpen(false); }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (Instance == this) { if (IsOpen) SetOpen(false); Instance = null; }
    }

    public void SetOpen(bool open)
    {
        if (open == IsOpen) return;
        if (open)
        {
            if (StageRewardController.InputBlocked || Time.timeScale <= 0f) return;
            if (player == null) player = FindObjectOfType<Player>();
            if (itemManager == null) itemManager = FindObjectOfType<ItemManager>();
            if (player == null || itemManager == null || GameManager.player_current_HP <= 0f ||
                player.on_dash || player.skill_using ||
                (player.on_attack && (player.chargeBow == null || !player.chargeBow.IsCharging))) return;
            if (player.chargeBow != null) player.chargeBow.CancelCharge();
            priorTimeScale = Time.timeScale;
            priorCursorLock = Cursor.lockState;
            priorCursorVisible = Cursor.visible;
            IsOpen = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshWeapons();
            status.text = "원하는 무기를 클릭하면 즉시 획득하고 장착합니다.";
            panel.SetActive(true);
        }
        else
        {
            IsOpen = false;
            blockThroughFrame = Time.frameCount + 1;
            panel.SetActive(false);
            Time.timeScale = priorTimeScale;
            Cursor.lockState = priorCursorLock;
            Cursor.visible = priorCursorVisible;
        }
    }

    public void RefreshWeapons()
    {
        AvailableWeapons.Clear();
        if (itemManager != null)
            foreach (var item in itemManager.GetItemList())
                if (item != null && item.itemType == ItemType.Weapon &&
                    !AvailableWeapons.Contains(item)) AvailableWeapons.Add(item);
        page = Mathf.Clamp(page, 0, Mathf.Max(0, (AvailableWeapons.Count - 1) / PageSize));
        RenderPage();
    }

    public bool GrantWeapon(Item item)
    {
        if (!IsOpen || item == null || !AvailableWeapons.Contains(item) || player == null) return false;
        bool success = player.EquipDeveloperWeapon(item);
        status.text = success ? item.itemName + " 획득 · 장착 완료" : "이 무기는 현재 장착할 수 없습니다.";
        RenderPage();
        return success;
    }

    void RenderPage()
    {
        for (int i = 0; i < PageSize; i++)
        {
            int index = page * PageSize + i;
            bool exists = index < AvailableWeapons.Count;
            buttons[i].gameObject.SetActive(exists);
            if (!exists) continue;
            var item = AvailableWeapons[index];
            icons[i].sprite = item.icon;
            icons[i].enabled = item.icon != null;
            bool equipped = player != null && (item.IsBow ? player.EquippedBowItem == item :
                player.sword_num == item.value);
            names[i].text = (item.IsBow ? "활" : "검") + " · " + item.itemName +
                (equipped ? "\n<color=#78E5C2>장착 중</color>" : "");
        }
        int pages = Mathf.Max(1, (AvailableWeapons.Count + PageSize - 1) / PageSize);
        pageLabel.text = "전체 무기 " + AvailableWeapons.Count + "종   ·   " + (page + 1) + " / " + pages;
        previous.interactable = page > 0;
        next.interactable = page + 1 < pages;
    }

    void BuildPanel()
    {
        var canvas = FeatureUI.Canvas("DeveloperWeaponCanvas", transform, 300);
        panel = FeatureUI.Image("Modal", canvas.transform, Vector2.zero, Vector2.zero,
            new Color(.015f, .025f, .045f, .92f)).gameObject;
        var full = (RectTransform)panel.transform;
        full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
        full.offsetMin = full.offsetMax = Vector2.zero;
        panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        var body = FeatureUI.Image("Inventory", panel.transform, new Vector2(1440, 900), Vector2.zero,
            new Color(.055f, .075f, .105f, 1f));
        FeatureUI.Text("Title", body.transform, new Vector2(1000, 64), new Vector2(0, 385),
            "개발자 무기 인벤토리", 42, font, Color.white);
        FeatureUI.Text("Hint", body.transform, new Vector2(1100, 45), new Vector2(0, 330),
            "슬래시(/) / ESC 닫기  ·  게임 일시 정지", 22, font, new Color(.6f,.72f,.85f));
        var close = FeatureUI.Button("Close", body.transform, new Vector2(80, 55), new Vector2(650,385),
            new Color(.22f,.28f,.36f));
        FeatureUI.Text("Label", close.transform, new Vector2(75,50), Vector2.zero, "닫기", 22, font, Color.white);
        close.onClick.AddListener(() => SetOpen(false));
        for (int i = 0; i < PageSize; i++)
        {
            int slot = i;
            var button = FeatureUI.Button("Weapon" + i, body.transform, new Vector2(326,128),
                new Vector2(-510 + (i % 4) * 340, 225 - (i / 4) * 142),
                new Color(.12f,.17f,.23f));
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
            var icon = FeatureUI.Image("Icon",button.transform,new Vector2(72,88),new Vector2(-112,0),Color.white);
            icon.preserveAspect = true;
            var label = FeatureUI.Text("Name",button.transform,new Vector2(220,106),new Vector2(43,0),
                "",24,font,Color.white);
            label.enableAutoSizing = true; label.fontSizeMin=17; label.fontSizeMax=24;
            buttons.Add(button); icons.Add(icon); names.Add(label);
            button.onClick.AddListener(() => {
                int index=page*PageSize+slot;
                if(index<AvailableWeapons.Count) GrantWeapon(AvailableWeapons[index]);
            });
        }
        previous=FeatureUI.Button("Previous",body.transform,new Vector2(105,46),new Vector2(-240,-320),new Color(.17f,.24f,.32f));
        next=FeatureUI.Button("Next",body.transform,new Vector2(105,46),new Vector2(240,-320),new Color(.17f,.24f,.32f));
        FeatureUI.Text("Label",previous.transform,new Vector2(100,44),Vector2.zero,"이전",23,font,Color.white);
        FeatureUI.Text("Label",next.transform,new Vector2(100,44),Vector2.zero,"다음",23,font,Color.white);
        previous.onClick.AddListener(()=>{page--; RenderPage();});
        next.onClick.AddListener(()=>{page++; RenderPage();});
        pageLabel=FeatureUI.Text("Page",body.transform,new Vector2(360,45),new Vector2(0,-320),"",22,font,Color.white);
        status=FeatureUI.Text("Status",body.transform,new Vector2(1300,60),new Vector2(0,-390),"",25,font,new Color(.47f,.9f,.76f));
        panel.SetActive(false);
    }
}
