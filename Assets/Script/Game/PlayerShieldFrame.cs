using UnityEngine;

// Matches the sprite's visible (unpadded) health bar, then clips its border from right to left like the health fill.
public sealed class PlayerShieldFrame : MonoBehaviour
{
    public UnityEngine.UI.Image healthImage;
    public RectTransform frame;
    public UnityEngine.UI.Image[] edges = new UnityEngine.UI.Image[4];
    public float borderPixels = 2f;
    public float FillFraction => Mathf.Clamp01(RunUpgradeState.Shield / RunUpgradeState.ShieldCapacity);
    void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if (healthImage == null || frame == null || edges == null || edges.Length != 4) return;
        var sprite = healthImage.sprite;
        var padding = sprite != null ? UnityEngine.Sprites.DataUtility.GetPadding(sprite) : Vector4.zero;
        var size = sprite != null ? sprite.rect.size : Vector2.one;
        frame.anchorMin = new Vector2(padding.x / size.x, padding.y / size.y);
        frame.anchorMax = new Vector2(1f - padding.z / size.x, 1f - padding.w / size.y);
        float scaleX = Mathf.Max(.001f, Mathf.Abs(frame.lossyScale.x));
        float scaleY = Mathf.Max(.001f, Mathf.Abs(frame.lossyScale.y));
        frame.sizeDelta = new Vector2(borderPixels * 2f / scaleX, borderPixels * 2f / scaleY);
        frame.anchoredPosition = Vector2.zero;
        frame.gameObject.SetActive(RunUpgradeState.Shield > 0f && GameManager.player_current_HP > 0f);
        float width = Mathf.Max(.01f, frame.rect.width), height = Mathf.Max(.01f, frame.rect.height);
        float visibleWidth = width * FillFraction;
        float verticalThickness = borderPixels / scaleX;
        float horizontalThickness = borderPixels / scaleY;
        for (int i=0;i<4;i++)
        {
            var edge = edges[i];
            if (edge == null) continue;
            var r = edge.rectTransform;
            bool horizontal = i % 2 == 0;
            float pieceWidth = horizontal ? visibleWidth : i == 3 ?
                Mathf.Min(verticalThickness, visibleWidth) :
                Mathf.Clamp(visibleWidth - (width - verticalThickness), 0f, verticalThickness);
            edge.fillAmount = FillFraction;
            edge.enabled = pieceWidth > 0f;
            var corner = new Vector2(0f, i < 2 ? 1f : 0f);
            r.anchorMin = r.anchorMax = r.pivot = corner;
            r.anchoredPosition = new Vector2(i == 1 ? width - verticalThickness : 0f, 0f);
            r.sizeDelta = new Vector2(pieceWidth, horizontal ? horizontalThickness : height);
        }
    }
}
