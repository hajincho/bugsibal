using UnityEngine;

public class HPNum : MonoBehaviour
{
    public UnityEngine.UI.Text hpText;
    public static string FormattedHealth()
    {
        string health = GameManager.player_current_HP.ToString("0.0");
        string maximum = GameManager.player_HP.ToString("0.0");
        if (RunUpgradeState.Shield > 0f)
            return health + " <color=#FFDC45>+ " + RunUpgradeState.Shield.ToString("0.0") + "</color> / " + maximum;
        return health + " / " + maximum;
    }
    void Update()
    {
        if (hpText == null) return;
        hpText.supportRichText = true;
        hpText.text = FormattedHealth();
    }
}
