using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CombatFeatureChecks
{
    static void Check(bool result,string message,List<string> passed)
    {if(!result)throw new Exception("Feature check failed: "+message);passed.Add(message);}
    static bool Near(float a,float b){return Mathf.Abs(a-b)<.001f;}

    public static string[] RunDataChecks()
    {
        var passed=new List<string>();
        var config=Resources.Load<CombatFeatureConfig>("CombatRewards/Config");
        Check(config!=null && config.rewards.Length==5,"Five reward definitions are available",passed);
        var original=RunUpgradeState.Capture();
        float hp=GameManager.player_HP,current=GameManager.player_current_HP;
        int power=GameManager.player_power,skill=GameManager.player_skill_power;
        GameObject target=null;
        try
        {
            GameManager.ResetRun();
            var heal=config.rewards.First(r=>r.kind==StageRewardKind.Heal);
            var attack=config.rewards.First(r=>r.kind==StageRewardKind.Power);
            var speed=config.rewards.First(r=>r.kind==StageRewardKind.AttackSpeed);
            var health=config.rewards.First(r=>r.kind==StageRewardKind.MaxHealth);
            var move=config.rewards.First(r=>r.kind==StageRewardKind.MoveSpeed);
            Check(!RunUpgradeState.CanOffer(heal),"Full health excludes healing reward",passed);
            GameManager.player_current_HP=70;
            Check(RunUpgradeState.TryClaim("heal",heal) && Near(GameManager.player_current_HP,90),"Healing restores 20 percent of maximum HP",passed);
            Check(!RunUpgradeState.TryClaim("heal",heal) && Near(GameManager.player_current_HP,90),"A stage cannot grant a reward twice",passed);
            Check(RunUpgradeState.TryClaim("power",attack) && Near(RunUpgradeState.NormalPower,10.5f),"Five percent attack bonus retains fractional damage",passed);
            for(int i=0;i<9;i++)RunUpgradeState.TryClaim("speed"+i,speed);
            Check(Near(RunUpgradeState.AttackSpeedBonus,.3f) && !RunUpgradeState.CanOffer(speed),"Attack speed caps at thirty percent",passed);
            for(int i=0;i<8;i++)RunUpgradeState.TryClaim("move"+i,move);
            Check(Near(RunUpgradeState.MoveSpeedBonus,.15f) && !RunUpgradeState.CanOffer(move),"Movement speed caps at fifteen percent",passed);
            RunUpgradeState.TryClaim("health",health);
            Check(Near(GameManager.player_HP,110) && Near(GameManager.player_current_HP,100),"Health reward grants maximum and current HP by ten",passed);
            var snapshot=RunUpgradeState.Capture();
            RunUpgradeState.TryClaim("extra",health);RunUpgradeState.Restore(snapshot);
            Check(Near(GameManager.player_HP,110) && !RunUpgradeState.WasClaimed("extra"),"Retry snapshot restores bonuses and claim ledger",passed);
            GameManager.player_current_HP=1;
            Check(Near(GameManager.DamagePlayer(1000),1) && Near(GameManager.player_current_HP,0),"Player lethal damage is clamped to actual HP lost",passed);
            Check(Near(GameManager.HealPlayer(100),0),"Healing cannot revive a defeated player",passed);
            target=new GameObject("CombatValidationTarget");var enemy=target.AddComponent<EnemyData>();enemy.enemy_HP=enemy.enemy_current_HP=10;
            Check(Near(enemy.TakeDamage(25),10) && Near(enemy.enemy_current_HP,0),"Enemy lethal damage reports actual HP lost",passed);
            Check(Near(enemy.TakeDamage(4),0),"Defeated enemies cannot receive duplicate damage",passed);
            enemy.enemy_current_HP=5;Check(Near(enemy.Heal(20),5) && Near(enemy.enemy_current_HP,10),"Enemy healing is capped at maximum HP",passed);
            Check(config.font.HasCharacters("스테이지 클리어! 일반·스킬 공격력 체력 회복 선택하고 다음 스테이지로 중간 보스 최종 보스 0123456789→+%"),"Korean UI and numeric glyphs are baked",passed);
            GameManager.ResetRun();
            Check(Near(RunUpgradeState.PowerBonus,0) && Near(RunUpgradeState.HealthBonus,0) && !RunUpgradeState.WasClaimed("power"),"New game resets bonuses and reward claims",passed);
        }
        finally
        {
            if(target!=null)UnityEngine.Object.DestroyImmediate(target);
            RunUpgradeState.Restore(original);GameManager.player_HP=hp;GameManager.player_current_HP=current;GameManager.player_power=power;GameManager.player_skill_power=skill;
        }
        return passed.ToArray();
    }
}
