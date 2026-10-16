using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Small helpers for authoring uGUI + TextMeshPro hierarchies from editor code.</summary>
    internal static class UiKit
    {
        public static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.5f);
        public static readonly Color ButtonColor = new Color(1f, 0.82f, 0.35f);
        public static readonly Color TextDark = new Color(0.15f, 0.12f, 0.1f);

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            return Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        public static Image AddImage(RectTransform rect, Color color, bool raycastTarget = true)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        public static TextMeshProUGUI AddText(RectTransform rect, string text, float size, TextAlignmentOptions alignment,
            Color color, FontStyles style = FontStyles.Normal)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>Stretched label as a child, so the parent can carry an Image or Button.</summary>
        public static TextMeshProUGUI ChildText(Transform parent, string name, string text, float size,
            TextAlignmentOptions alignment, Color color, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = Stretch(name, parent);
            return AddText(rect, text, size, alignment, color, style);
        }

        public static Button AddButton(RectTransform rect, Color color, out Image image)
        {
            image = AddImage(rect, color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
