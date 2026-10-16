using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Object;

// Editor-only integration probe. Changes are confined to play mode and are not saved to scenes.
public sealed class CombatFeaturePlayChecks
{
    [Serializable] public class Report { public List<string> passed=new List<string>();public string error;public bool complete; }
    const string Output=@"C:\Users\srobo\Documents\Codex\2026-10-16\x20-1-2-3-4-5-5\outputs";
    readonly Report report=new Report();
    float originalBackground;
    static CombatFeaturePlayChecks active;
    IEnumerator routine;
    public static string Begin()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter play mode first.");
        if(active!=null)return "Already running";
        active=new CombatFeaturePlayChecks();active.Start();EditorApplication.update+=active.Pump;return "Integration checks started";
    }
    void Start(){originalBackground=Application.runInBackground?1:0;Application.runInBackground=true;Directory.CreateDirectory(Output);routine=Guarded();}
    void Pump()
    {
        if(!EditorApplication.isPlaying){report.error="Play validation interrupted before completion";Save();EditorApplication.update-=Pump;active=null;return;}
        if(!routine.MoveNext()){EditorApplication.update-=Pump;active=null;}
    }
    void Check(bool value,string name){if(!value)throw new Exception(name);report.passed.Add(name);Save();}
    bool Near(float a,float b){return Mathf.Abs(a-b)<.01f;}
    void Save(){File.WriteAllText(Path.Combine(Output,"unity-validation.json"),JsonUtility.ToJson(report,true));}
    IEnumerator Guarded()
    {
        var work=Run();
        while(true)
        {
            bool next;
            try{next=work.MoveNext();}
            catch(Exception e){report.error=e.ToString();Save();Debug.LogException(e);break;}
            if(!next){report.complete=true;Save();break;}
            yield return work.Current;
        }
        Application.runInBackground=originalBackground>0;
        Time.timeScale=1;
        EditorApplication.delayCall+=()=>{EditorApplication.isPlaying=false;};
    }
    void Screenshot(string name){ScreenCapture.CaptureScreenshot(Path.Combine(Output,name+".png"));}
    IEnumerator Run()
    {
        for(int i=0;i<3;i++)yield return null;
        Check(StageRewardController.Instance!=null && CombatPresentation.Instance!=null,"Feature root and both UI systems initialize in play mode");
        GameManager.ResetRun();Time.timeScale=1;
        var first=FindObjectsOfType<Portal>(true).FirstOrDefault(p=>p.next_stage=="Stage1");
        if(first==null)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefep/Portal/PortalToStage2.prefab");
            first=Instantiate(source).GetComponent<Portal>();first.next_stage="Stage1";first.nextstage=1;
        }
        first.gameObject.SetActive(true);yield return null;first.Enter();
        for(int i=0;i<3;i++)yield return null;
        Check(SceneManager.GetActiveScene().name=="Stage1" && !StageRewardController.Instance.IsOpen,"Starting portal enters Stage1 without a reward");
        var stage=FindObjectOfType<StageManager>();Check(stage!=null,"Stage1 uses existing StageManager");
        var targetPrefab=stage.enemies_phase1.First();
        var phases=new HashSet<int>();float deadline=Time.realtimeSinceStartup+25;Time.timeScale=5;
        while(stage.phase<4 && Time.realtimeSinceStartup<deadline)
        {
            phases.Add(stage.phase);GameManager.player_current_HP=GameManager.player_HP;
            foreach(var obj in stage.summoned_enemies.ToArray())
            {if(obj==null)continue;var e=obj.GetComponent<EnemyData>()??obj.GetComponentInChildren<EnemyData>();if(e!=null && e.enemy_current_HP>0)e.TakeDamage(e.enemy_current_HP+1,CombatDamageKind.Skill);}
            yield return null;
        }
        Time.timeScale=1;
        Check(stage.phase==4 && StageRewardController.Instance.StageCleared,"All three waves finish and mark the stage cleared");
        Check(phases.Contains(0)&&phases.Contains(1)&&phases.Contains(2)&&phases.Contains(3),"Integration observed each wave boundary 0,1,2,3");
        GameManager.player_current_HP=60;
        var portal=FindObjectsOfType<Portal>().First(p=>p.next_stage=="Stage2");portal.Enter();yield return null;
        var controller=StageRewardController.Instance;
        Check(controller.IsOpen && Near(Time.timeScale,0) && StageRewardController.InputBlocked,"Portal opens reward panel and blocks game time and input");
        Check(controller.Offered.Length==3 && controller.Offered.Select(r=>r.kind).Distinct().Count()==3,"Three unique eligible reward cards are offered");
        Check(FindObjectsOfType<UnityEngine.EventSystems.EventSystem>().Length==1,"Reward selection has exactly one EventSystem");
        Screenshot("stage-reward-preview");for(int i=0;i<4;i++)yield return null;
        int index=Array.FindIndex(controller.Offered,r=>r.kind==StageRewardKind.MaxHealth);if(index<0)index=0;
        var chosen=controller.Offered[index];float priorHP=GameManager.player_HP,priorCurrent=GameManager.player_current_HP;
        GameObject.Find("RewardCard"+(index+1)).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Check(GameObject.Find("ConfirmReward").GetComponent<UnityEngine.UI.Button>().interactable,"Selecting a card enables the confirmation button");
        GameObject.Find("ConfirmReward").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();controller.Confirm();
        for(int i=0;i<4;i++)yield return null;
        Check(SceneManager.GetActiveScene().name=="Stage2" && !controller.IsOpen && Near(Time.timeScale,1),"Confirming the UI reward transitions to Stage2 and resumes time");
        Check(RunUpgradeState.WasClaimed("Stage1") && !RunUpgradeState.TryClaim("Stage1",chosen),"Repeated confirmation cannot grant the Stage1 reward twice");
        if(chosen.kind==StageRewardKind.MaxHealth)Check(Near(GameManager.player_HP,priorHP+10) && GameManager.player_current_HP<=priorCurrent+10,"Maximum health reward survives the scene transition");
        var config=controller.config;var baseline=RunUpgradeState.Capture();
        var power=config.rewards.First(r=>r.kind==StageRewardKind.Power);var health=config.rewards.First(r=>r.kind==StageRewardKind.MaxHealth);
        RunUpgradeState.TryClaim("ValidationPower",power);RunUpgradeState.TryClaim("ValidationHealth",health);
        var manager=FindObjectOfType<GameManager>();manager.AddItemValue();float max=GameManager.player_HP;
        manager.AddItemValue();Check(Near(RunUpgradeState.NormalPower,GameManager.player_power*(1+RunUpgradeState.PowerBonus)) && Near(max,GameManager.player_HP),"Repeated equipment recalculation preserves upgrades without stacking them twice");
        FindObjectOfType<GameOverBG>().Restart();for(int i=0;i<4;i++)yield return null;
        Check(Near(RunUpgradeState.PowerBonus,baseline.power)&&Near(RunUpgradeState.HealthBonus,baseline.health)&&!RunUpgradeState.WasClaimed("ValidationPower"),"Actual restart restores Stage2 entry upgrades and claim ledger");
        Check(RunUpgradeState.WasClaimed("Stage1"),"Restart preserves the reward earned in the previous stage");

        // Isolate target collisions while retaining the existing attack and skill scripts.
        var target=Instantiate(targetPrefab);target.name="ValidationEnemy";
        foreach(var script in target.GetComponentsInChildren<MonoBehaviour>())if(!(script is EnemyData)&&!(script is IsSkillDamaged))script.enabled=false;
        foreach(var rb in target.GetComponentsInChildren<Rigidbody2D>())rb.simulated=false;
        var enemy=target.GetComponent<EnemyData>()??target.GetComponentInChildren<EnemyData>();enemy.enemy_HP=1000;
        var character=GameObject.FindGameObjectWithTag("Player");target.transform.position=character.transform.position+new Vector3(3,2,0);
        var collider=target.GetComponentInChildren<Collider2D>();collider.enabled=false;
        var flags=enemy.GetComponent<IsSkillDamaged>();if(flags==null)flags=enemy.gameObject.AddComponent<IsSkillDamaged>();
        for(int i=0;i<2;i++)yield return null;
        var player=character.GetComponent<Player>();int oldMode=player.weapon_mode;player.weapon_mode=1;player.on_attack=true;
        var attackObject=new GameObject("ValidationSwordHit");var hit=attackObject.AddComponent<PlayerAttack>();
        float before=enemy.enemy_current_HP;hit.SendMessage("OnTriggerEnter2D",collider);
        Check(Near(before-enemy.enemy_current_HP,RunUpgradeState.NormalPower),"Existing sword hit applies and displays normal attack damage");
        float afterFirst=enemy.enemy_current_HP;hit.SendMessage("OnTriggerEnter2D",collider);
        Check(Near(enemy.enemy_current_HP,afterFirst),"Sword duplicate collider hit respects its original hit lock");
        player.on_attack=false;player.weapon_mode=0;
        var arrowObject=new GameObject("ValidationArrow",typeof(Rigidbody2D));arrowObject.GetComponent<Rigidbody2D>().simulated=false;
        arrowObject.AddComponent<Arrow>();var arrowHit=arrowObject.AddComponent<PlayerAttack>();
        before=enemy.enemy_current_HP;arrowHit.SendMessage("OnTriggerEnter2D",collider);arrowHit.SendMessage("OnTriggerEnter2D",collider);
        Check(Near(before-enemy.enemy_current_HP,RunUpgradeState.NormalPower*.5f),"Arrow damage works after the attack animation and hits each target once");
        var skillObject=new GameObject("ValidationSkill");var explosion=skillObject.AddComponent<ExplosionFb>();
        before=enemy.enemy_current_HP;explosion.SendMessage("OnTriggerEnter2D",collider);
        Check(Near(before-enemy.enemy_current_HP,RunUpgradeState.SkillPower+20),"Existing fireball explosion applies skill damage through the shared path");
        var ultObject=new GameObject("ValidationUltimate");var slash=ultObject.AddComponent<Slash>();
        before=enemy.enemy_current_HP;slash.SendMessage("OnTriggerEnter2D",collider);
        Check(Near(before-enemy.enemy_current_HP,RunUpgradeState.SkillPower*2+RunUpgradeState.NormalPower*2+20),"Existing ultimate applies its combined damage through the shared path");
        player.weapon_mode=oldMode;Destroy(attackObject);Destroy(arrowObject);
        for(int i=0;i<3;i++)yield return null;
        GameManager.player_current_HP=GameManager.player_HP;enemy.TakeDamage(37,CombatDamageKind.Skill);GameManager.DamagePlayer(12);Time.timeScale=0;
        for(int i=0;i<3;i++)yield return null;
        var labels=CombatPresentation.Instance.GetComponentsInChildren<TMPro.TMP_Text>(false).Select(t=>t.text).ToArray();
        Check(labels.Any(t=>t=="37") && labels.Any(t=>t=="12"),"Enemy and player damage numbers both render");
        Check(labels.Any(t=>t.Contains(" / 1000")),"Normal enemy remaining HP text renders");
        Screenshot("damage-and-health-preview");for(int i=0;i<4;i++)yield return null;
        Time.timeScale=1;Destroy(target);

        SceneManager.LoadScene("MidBoss");for(int i=0;i<4;i++)yield return null;
        deadline=Time.realtimeSinceStartup+15;EnemyData boss=null;
        while(boss==null && Time.realtimeSinceStartup<deadline){GameManager.player_current_HP=GameManager.player_HP;boss=FindObjectsOfType<EnemyData>().FirstOrDefault(e=>e.IsBoss);yield return null;}
        Check(boss!=null,"MidBoss encounter registers its actual boss");
        foreach(var script in boss.GetComponentsInChildren<MonoBehaviour>())if(!(script is EnemyData)&&!(script is IsSkillDamaged))script.enabled=false;
        foreach(var rb in boss.GetComponentsInChildren<Rigidbody2D>())rb.simulated=false;
        boss.TakeDamage(23,CombatDamageKind.Skill);yield return null;
        var hud=GameObject.Find("BossHealthHUD").GetComponent<RectTransform>();Vector2 anchored=hud.anchoredPosition;
        Check(hud.anchorMin==new Vector2(.5f,1) && hud.anchorMax==new Vector2(.5f,1),"Boss HP bar uses a screen-top anchor");
        boss.transform.position+=new Vector3(4,2,0);Camera.main.transform.position+=new Vector3(2,1,0);yield return null;
        Check(hud.anchoredPosition==anchored,"Boss and camera movement do not move the boss HUD");
        Time.timeScale=0;Screenshot("midboss-health-preview");for(int i=0;i<4;i++)yield return null;
        boss.TakeDamage(boss.enemy_current_HP+1);yield return null;
        Check(!hud.gameObject.activeSelf,"Boss HP bar hides on boss defeat");Time.timeScale=1;

        SceneManager.LoadScene("FinalBoss");for(int i=0;i<5;i++)yield return null;
        boss=FindObjectsOfType<EnemyData>().FirstOrDefault(e=>e.IsBoss);Check(boss!=null,"FinalBoss encounter registers its actual boss");
        foreach(var script in boss.GetComponentsInChildren<MonoBehaviour>())if(!(script is EnemyData)&&!(script is IsSkillDamaged))script.enabled=false;
        boss.TakeDamage(225,CombatDamageKind.Skill);Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
        Check(GameObject.Find("BossName").GetComponent<TMPro.TMP_Text>().text=="최종 보스","FinalBoss uses the shared named boss HUD");
        Screenshot("finalboss-health-preview");for(int i=0;i<4;i++)yield return null;
        var legacy=FindObjectsOfType<BossHP>(true).FirstOrDefault();
        Check(legacy==null || !legacy.HP.gameObject.activeInHierarchy,"Legacy boss bar is hidden so the fixed HUD is not duplicated");
        Time.timeScale=1;GameManager.ResetRun();
        Check(Near(RunUpgradeState.PowerBonus,0)&&Near(RunUpgradeState.HealthBonus,0)&&!RunUpgradeState.WasClaimed("Stage1"),"New-game reset clears rewards after the full integration sequence");
    }
}

[InitializeOnLoad]
public static class CombatFeaturePlayCheckLauncher
{
    const string Request="Library/CombatFeatureValidation.request";
    const string Phase="CombatFeature.ValidationLaunchPhase";
    static CombatFeaturePlayCheckLauncher(){EditorApplication.update+=Pump;}
    static void Pump()
    {
        if(!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
        string phase=SessionState.GetString(Phase,"");
        if(phase=="")
        {
            SessionState.SetString(Phase,"stop");
            if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
            phase="stop";
        }
        if(phase=="stop")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            SessionState.SetString(Phase,"start");EditorApplication.isPlaying=true;return;
        }
        if(!EditorApplication.isPlaying)return;
        Application.runInBackground=true;
        if(StageRewardController.Instance==null)return;
        File.Delete(Request);SessionState.EraseString(Phase);CombatFeaturePlayChecks.Begin();
    }
}
