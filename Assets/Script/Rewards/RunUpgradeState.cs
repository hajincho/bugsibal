using System;
using System.Collections.Generic;
using UnityEngine;

public static class RunUpgradeState
{
    // Flat damage added to sword normal attacks and the bow's base arrow damage.
    public static float PowerBonus { get; private set; }
    public static float AttackSpeedBonus { get; private set; }
    public static float HealthBonus { get; private set; }
    public static float MoveSpeedBonus { get; private set; }
    public const float ShieldCapacity = 50f;
    public static float Shield { get; private set; }
    public static float NormalPower => GameManager.player_power + PowerBonus;
    public static float SkillPower => GameManager.player_skill_power;
    public static float AttackRate => Mathf.Max(.01f, 1f + AttackSpeedBonus + GameManager.player_sword_attack_speed / 100f);
    public static float BowChargeRate => 1f + AttackSpeedBonus;
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
        Shield = 0f;
        claimed.Clear();
        snapshotScene = null;
        snapshot = default(Snapshot);
    }

    public static void GrantShield(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount)) return;
        Shield = Mathf.Clamp(amount, 0f, ShieldCapacity);
    }

    public static float AbsorbWithShield(float damage)
    {
        float absorbed = Mathf.Min(Shield, Mathf.Max(0f, damage));
        Shield = Mathf.Max(0f, Shield - absorbed);
        return absorbed;
    }

    public static void ClearShield() { Shield = 0f; }

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
        // Shield is consumable: retrying a stage must not regenerate its snapshot value.
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
            case StageRewardKind.Heal: return GameManager.player_current_HP > 0 && GameManager.player_current_HP < GameManager.player_HP - .01f;
            case StageRewardKind.AttackSpeed: return AttackSpeedBonus < reward.cap - .001f;
            case StageRewardKind.MoveSpeed: return MoveSpeedBonus < reward.cap - .001f;
            case StageRewardKind.Shield: return Shield < ShieldCapacity - .001f;
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
            case StageRewardKind.Shield: GrantShield(reward.amount); break;
        }
        return true;
    }

    public static string Preview(StageRewardDefinition reward)
    {
        switch (reward.kind)
        {
            case StageRewardKind.Heal: return string.Format("체력 {0:0.0} → {1:0.0}", GameManager.player_current_HP, Mathf.Min(GameManager.player_HP, GameManager.player_current_HP + GameManager.player_HP * reward.amount));
            case StageRewardKind.Power: return string.Format("일반 공격력 +{0:0.#} → +{1:0.#}\n활 기본 피해 +{0:0.#} → +{1:0.#}", PowerBonus, PowerBonus + reward.amount);
            case StageRewardKind.AttackSpeed: return string.Format("검 공격·활 차징 속도\n+{0:0}% → +{1:0}%", AttackSpeedBonus * 100, Mathf.Min(reward.cap, AttackSpeedBonus + reward.amount) * 100);
            case StageRewardKind.MaxHealth: return string.Format("최대 체력 {0:0.0} → {1:0.0}", GameManager.player_HP, GameManager.player_HP + reward.amount);
            case StageRewardKind.Shield: return string.Format("보호막 {0:0.0} → {1:0.0}\n체력보다 먼저 소모 · 재생 없음", Shield, Mathf.Clamp(reward.amount, 0f, ShieldCapacity));
            default: return string.Format("이동 속도 +{0:0}% → +{1:0}%", MoveSpeedBonus * 100, Mathf.Min(reward.cap, MoveSpeedBonus + reward.amount) * 100);
        }
    }
}
