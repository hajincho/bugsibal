using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class FeatureUI
{
    public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = pos;
        return rect;
    }

    public static Canvas Canvas(string name, Transform parent, int order)
    {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        var canvas = rect.gameObject.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = rect.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        rect.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return canvas;
    }

    public static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 size, Vector2 pos, Color color, Sprite sprite = null)
    {
        var rect = Rect(name, parent, size, pos);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color; image.sprite = sprite; image.raycastTarget = false;
        if (sprite != null && sprite.border.sqrMagnitude > 0) image.type = UnityEngine.UI.Image.Type.Sliced;
        return image;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, Vector2 size, Vector2 pos, string value, float sizePt, TMP_FontAsset font, Color color)
    {
        var rect = Rect(name, parent, size, pos);
        var text = rect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        text.font = font; text.fontSize = sizePt; text.text = value;
        text.color = color; text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false; text.raycastTarget = false;
        return text;
    }

    public static UnityEngine.UI.Button Button(string name, Transform parent, Vector2 size, Vector2 pos, Color color)
    {
        var image = Image(name, parent, size, pos, color);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(.8f, .9f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        return button;
    }
}
