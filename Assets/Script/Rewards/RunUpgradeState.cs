using System;
using System.Collections.Generic;
using UnityEngine;

public static class RunUpgradeState
{
    public static float PowerBonus { get; private set; }
    public static float AttackSpeedBonus { get; private set; }
    public static float HealthBonus { get; private set; }
    public static float MoveSpeedBonus { get; private set; }
    public static float NormalPower => GameManager.player_power * (1f + PowerBonus);
    public static float SkillPower => GameManager.player_skill_power * (1f + PowerBonus);
    public static float AttackRate => 1f + AttackSpeedBonus;
    public static float MoveSpeed => GameManager.player_speed * (1f + MoveSpeedBonus);
    static readonly HashSet<string> claimed = new HashSet<string>();
    static string snapshotScene;
    static Snapshot snapshot;

    [Serializable]
    public struct Snapshot
    {
        public float power, attackSpeed, health, moveSpeed;
        public string[] claims;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Reset(); }

    public static void Reset()
    {
        GameManager.player_HP = Mathf.Max(1f, GameManager.player_HP - HealthBonus);
        PowerBonus = AttackSpeedBonus = HealthBonus = MoveSpeedBonus = 0f;
        claimed.Clear();
        snapshotScene = null;
        snapshot = default(Snapshot);
    }

    public static Snapshot Capture()
    {
        var claims = new string[claimed.Count];
        claimed.CopyTo(claims);
        return new Snapshot { power=PowerBonus, attackSpeed=AttackSpeedBonus, health=HealthBonus, moveSpeed=MoveSpeedBonus, claims=claims };
    }

    public static void Restore(Snapshot state)
    {
        float delta = state.health - HealthBonus;
        PowerBonus = state.power; AttackSpeedBonus = state.attackSpeed;
        HealthBonus = state.health; MoveSpeedBonus = state.moveSpeed;
        GameManager.player_HP = Mathf.Max(1, GameManager.player_HP + delta);
        GameManager.player_current_HP = Mathf.Clamp(GameManager.player_current_HP + delta, 0, GameManager.player_HP);
        claimed.Clear();
        if (state.claims != null) foreach (string key in state.claims) claimed.Add(key);
    }

    public static void EnterStage(string scene)
    {
        if (snapshotScene == scene) Restore(snapshot);
        else { snapshotScene = scene; snapshot = Capture(); }
    }

    public static bool WasClaimed(string stage) { return claimed.Contains(stage); }
    public static bool CanOffer(StageRewardDefinition reward)
    {
        if (reward == null) return false;
        switch (reward.kind)
        {
            case StageRewardKind.Heal: return GameManager.player_current_HP > 0 && GameManager.player_current_HP < GameManager.player_HP - 0.01f;
            case StageRewardKind.AttackSpeed: return AttackSpeedBonus < reward.cap - 0.001f;
            case StageRewardKind.MoveSpeed: return MoveSpeedBonus < reward.cap - 0.001f;
            default: return true;
        }
    }

    public static bool TryClaim(string stage, StageRewardDefinition reward)
    {
        if (string.IsNullOrEmpty(stage) || claimed.Contains(stage) || !CanOffer(reward) || GameManager.player_current_HP <= 0) return false;
        claimed.Add(stage);
        switch (reward.kind)
        {
            case StageRewardKind.Heal: GameManager.HealPlayer(GameManager.player_HP * reward.amount); break;
            case StageRewardKind.Power: PowerBonus += reward.amount; break;
            case StageRewardKind.AttackSpeed: AttackSpeedBonus = Mathf.Min(reward.cap, AttackSpeedBonus + reward.amount); break;
            case StageRewardKind.MaxHealth:
                HealthBonus += reward.amount;
                GameManager.player_HP += reward.amount;
                GameManager.HealPlayer(reward.amount);
                break;
            case StageRewardKind.MoveSpeed: MoveSpeedBonus = Mathf.Min(reward.cap, MoveSpeedBonus + reward.amount); break;
        }
        return true;
    }

    public static string Preview(StageRewardDefinition reward)
    {
        switch (reward.kind)
        {
            case StageRewardKind.Heal: return string.Format("체력 {0:0.#} → {1:0.#}", GameManager.player_current_HP, Mathf.Min(GameManager.player_HP, GameManager.player_current_HP + GameManager.player_HP * reward.amount));
            case StageRewardKind.Power: return string.Format("공격력 {0:0.#} → {1:0.#}\n스킬 공격력 {2:0.#} → {3:0.#}", NormalPower, GameManager.player_power * (1 + PowerBonus + reward.amount), SkillPower, GameManager.player_skill_power * (1 + PowerBonus + reward.amount));
            case StageRewardKind.AttackSpeed: return string.Format("공격 속도 +{0:0}% → +{1:0}%", AttackSpeedBonus * 100, Mathf.Min(reward.cap, AttackSpeedBonus + reward.amount) * 100);
            case StageRewardKind.MaxHealth: return string.Format("최대 체력 {0:0.#} → {1:0.#}", GameManager.player_HP, GameManager.player_HP + reward.amount);
            default: return string.Format("이동 속도 +{0:0}% → +{1:0}%", MoveSpeedBonus * 100, Mathf.Min(reward.cap, MoveSpeedBonus + reward.amount) * 100);
        }
    }
}
