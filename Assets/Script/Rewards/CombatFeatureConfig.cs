using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "bugsibal/Combat Feature Config")]
public class CombatFeatureConfig : ScriptableObject
{
    public StageRewardDefinition[] rewards;
    public TMP_FontAsset font;
    public Sprite healthFill;
    public Sprite panelSprite;
}
