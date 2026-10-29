using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageRewardController : MonoBehaviour
{
    public CombatFeatureConfig config;
    public static StageRewardController Instance { get; private set; }
    public static bool InputBlocked => DeveloperWeaponInventory.InputBlocked || (Instance != null && (Instance.IsOpen || Instance.transitioning || Time.frameCount <= Instance.blockThroughFrame));
    public bool IsOpen { get; private set; }
    public bool StageCleared { get; private set; }
    public StageRewardDefinition[] Offered { get; private set; }
    GameObject panel;
    readonly List<Button> cards = new List<Button>();
    readonly List<TextMeshProUGUI> titles = new List<TextMeshProUGUI>();
    readonly List<TextMeshProUGUI> descriptions = new List<TextMeshProUGUI>();
    readonly List<TextMeshProUGUI> previews = new List<TextMeshProUGUI>();
    readonly List<UnityEngine.UI.Image> icons = new List<UnityEngine.UI.Image>();
    Button confirm;
    TextMeshProUGUI heading;
    Action proceed;
    string sourceScene;
    int selected = -1, blockThroughFrame = -1;
    bool transitioning;
    float priorTimeScale = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() { Ensure(); }

    public static StageRewardController Ensure()
    {
        if (Instance != null) return Instance;
        var prefab = Resources.Load<GameObject>("CombatRewards/CombatFeatureRoot");
        if (prefab != null) Instantiate(prefab);
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPanel();
        SceneManager.sceneLoaded += SceneLoaded;
        SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (Instance == this) { if (IsOpen) Time.timeScale = priorTimeScale; Instance = null; }
    }

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        if (IsOpen) ClosePanel();
        transitioning = false; StageCleared = false;
        blockThroughFrame = Time.frameCount + 1;
        if (IsCombatScene(scene.name)) RunUpgradeState.EnterStage(scene.name);
        if (CombatPresentation.Instance != null) CombatPresentation.Instance.SceneChanged();
    }

    public static bool IsCombatScene(string scene) { return scene.StartsWith("Stage", StringComparison.Ordinal) || scene == "MidBoss" || scene == "FinalBoss"; }
    public void MarkCleared() { StageCleared = true; }

    public bool NeedsReward(string nextScene)
    {
        string current = SceneManager.GetActiveScene().name;
        return IsCombatScene(current) && current != "FinalBoss" && IsCombatScene(nextScene) && !RunUpgradeState.WasClaimed(current);
    }

    public bool RequestTransition(string nextScene, Action next)
    {
        if (IsOpen || transitioning || GameManager.player_current_HP <= 0) return false;
        if (!NeedsReward(nextScene)) { transitioning = true; next(); return true; }
        if (!StageCleared) return false;
        sourceScene = SceneManager.GetActiveScene().name;
        proceed = next;
        var available = new List<StageRewardDefinition>();
        foreach (var reward in config.rewards) if (RunUpgradeState.CanOffer(reward)) available.Add(reward);
        for (int i = available.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var r=available[i]; available[i]=available[j]; available[j]=r; }
        Offered = available.GetRange(0, Mathf.Min(3, available.Count)).ToArray();
        if (Offered.Length == 0) { transitioning = true; next(); return true; }
        selected = -1; confirm.interactable = false;
        heading.text = "스테이지 클리어!";
        for (int i=0;i<cards.Count;i++)
        {
            cards[i].gameObject.SetActive(i<Offered.Length);
            if (i>=Offered.Length) continue;
            var r=Offered[i]; titles[i].text=r.title; descriptions[i].text=r.description; previews[i].text=RunUpgradeState.Preview(r);
            icons[i].sprite=r.icon; icons[i].enabled=r.icon!=null;
            cards[i].image.color = new Color(.12f,.16f,.22f);
        }
        priorTimeScale = Time.timeScale;
        IsOpen = true; panel.SetActive(true); Time.timeScale = 0;
        if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
        return true;
    }

    public void Select(int index)
    {
        if (!IsOpen || index<0 || index>=Offered.Length) return;
        selected=index; confirm.interactable=true;
        for(int i=0;i<cards.Count;i++) cards[i].image.color = i==index ? new Color(.21f,.35f,.44f) : new Color(.12f,.16f,.22f);
    }

    public void Confirm()
    {
        if (!IsOpen || selected<0 || transitioning) return;
        if (!RunUpgradeState.TryClaim(sourceScene, Offered[selected])) return;
        transitioning = true;
        var action=proceed;
        ClosePanel();
        action?.Invoke();
    }

    void ClosePanel()
    {
        IsOpen=false; panel.SetActive(false); proceed=null;
        Time.timeScale=priorTimeScale; blockThroughFrame=Time.frameCount+1;
        if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void TransitionFailed() { transitioning=false; blockThroughFrame=Time.frameCount+1; }

    void BuildPanel()
    {
        var canvas=FeatureUI.Canvas("StageRewardCanvas",transform,100);
        var shade=FeatureUI.Image("StageRewardPanel",canvas.transform,new Vector2(1920,1080),Vector2.zero,new Color(.015f,.025f,.045f,.94f));
        var rect=shade.rectTransform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=Vector2.zero;
        shade.raycastTarget=true; panel=shade.gameObject;
        FeatureUI.Image("RewardFrame",panel.transform,new Vector2(1280,720),new Vector2(0,10),new Color(.06f,.09f,.14f));
        heading=FeatureUI.Text("Heading",panel.transform,new Vector2(1100,75),new Vector2(0,290),"스테이지 클리어!",46,config.font,new Color(1,.85f,.48f));
        FeatureUI.Text("Instruction",panel.transform,new Vector2(1120,55),new Vector2(0,225),"보상 하나를 선택하세요 · 강화는 다음 스테이지에도 유지됩니다",25,config.font,Color.white);
        for(int i=0;i<3;i++)
        {
            int index=i;
            var button=FeatureUI.Button("RewardCard"+(i+1),panel.transform,new Vector2(350,355),new Vector2((i-1)*385,0),new Color(.12f,.16f,.22f));
            button.onClick.AddListener(()=>Select(index)); cards.Add(button);
            icons.Add(FeatureUI.Image("Icon",button.transform,new Vector2(76,76),new Vector2(0,102),Color.white));
            icons[i].preserveAspect=true;
            titles.Add(FeatureUI.Text("Title",button.transform,new Vector2(325,55),new Vector2(0,33),"",31,config.font,Color.white));
            descriptions.Add(FeatureUI.Text("Description",button.transform,new Vector2(315,88),new Vector2(0,-38),"",24,config.font,new Color(.83f,.88f,.94f)));
            previews.Add(FeatureUI.Text("Preview",button.transform,new Vector2(315,65),new Vector2(0,-118),"",22,config.font,new Color(.52f,.9f,.76f)));
        }
        confirm=FeatureUI.Button("ConfirmReward",panel.transform,new Vector2(420,70),new Vector2(0,-265),new Color(.2f,.43f,.37f));
        FeatureUI.Text("Label",confirm.transform,new Vector2(400,65),Vector2.zero,"선택하고 다음 스테이지로",27,config.font,Color.white);
        confirm.onClick.AddListener(Confirm);
        panel.SetActive(false);
    }
}
