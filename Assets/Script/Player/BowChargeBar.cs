using UnityEngine;

public sealed class BowChargeBar : MonoBehaviour
{
    public CanvasGroup visibility;
    public UnityEngine.UI.Image fill;
    public Bow source;
    public Color emptyColor = new Color(1f, 0.38f, 0.06f);
    public Color middleColor = new Color(1f, 0.88f, 0.08f);
    public Color fullColor = new Color(0.2f, 0.95f, 0.3f);

    void Awake() { Hide(); }

    void LateUpdate()
    {
        if (source != null && source.isActiveAndEnabled && source.IsBowMode)
            Show(source.ChargeSeconds, source.settings);
        else Hide();
    }

    public void Show(float seconds, BowChargeSettings settings)
    {
        if (settings == null) { Hide(); return; }
        if (visibility != null) visibility.alpha = 1f;
        float fraction = settings.ChargeFraction(seconds);
        if (fill != null)
        {
            fill.rectTransform.anchorMax = new Vector2(fraction, 1f);
            fill.color = fraction <= 0.5f
                ? Color.Lerp(emptyColor, middleColor, fraction * 2f)
                : Color.Lerp(middleColor, fullColor, (fraction - 0.5f) * 2f);
        }
    }

    public void Hide()
    {
        if (visibility != null) visibility.alpha = 0f;
        if (fill != null) fill.rectTransform.anchorMax = new Vector2(0f, 1f);
    }
}
