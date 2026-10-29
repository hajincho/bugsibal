using UnityEngine;

public enum StageRewardKind { Heal, Power, AttackSpeed, MaxHealth, MoveSpeed, Shield }

[CreateAssetMenu(menuName = "bugsibal/Stage Reward")]
public class StageRewardDefinition : ScriptableObject
{
    public StageRewardKind kind;
    public string title;
    [TextArea] public string description;
    public float amount;
    public float cap;
    public Sprite icon;
}
