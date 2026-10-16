using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>New season UI: the HUD button left of Trophies (clear of the build bar in landscape too) and its panel.</summary>
    internal static class SeasonUiBuilder
    {
        private static readonly Color Seashell = new Color(0.98f, 0.72f, 0.62f);
        private static readonly Color StartColor = new Color(0.45f, 0.8f, 0.45f);

        public static void Build(Transform parent)
        {
            RectTransform openRect = UiKit.Rect("SeasonButton", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(-250f, -580f), new Vector2(200f, 90f));
            Button open = UiKit.AddButton(openRect, Seashell, out _);
            TextMeshProUGUI openLabel = UiKit.ChildText(openRect, "Label", "New season", 30f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform host = UiKit.Stretch("SeasonPanel", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "New season", new Vector2(920f, 1120f), out RectTransform window, out Button close);
            TextMeshProUGUI summary = Text(window, "Summary", -160f, 270f, 40f, TextAlignmentOptions.Center);
            TextMeshProUGUI details = Text(window, "Details", -460f, 400f, 36f, TextAlignmentOptions.TopLeft);

            RectTransform startRect = UiKit.Rect("StartButton", window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -920f), new Vector2(640f, 130f));
            Button start = UiKit.AddButton(startRect, StartColor, out _);
            TextMeshProUGUI startLabel = UiKit.ChildText(startRect, "Label", "Start new season", 44f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var view = host.gameObject.AddComponent<SeasonPanelView>();
            PrimitiveKit.SetReference(view, "openButton", open);
            PrimitiveKit.SetReference(view, "openLabel", openLabel);
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "closeButton", close);
            PrimitiveKit.SetReference(view, "summary", summary);
            PrimitiveKit.SetReference(view, "details", details);
            PrimitiveKit.SetReference(view, "startButton", start);
            PrimitiveKit.SetReference(view, "startLabel", startLabel);
            root.gameObject.SetActive(false);
            openRect.gameObject.SetActive(false); // shown once the season tier is reached
        }

        private static TextMeshProUGUI Text(RectTransform window, string name, float y, float height, float size, TextAlignmentOptions alignment)
        {
            RectTransform rect = UiKit.Rect(name, window, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(-100f, height));
            TextMeshProUGUI label = UiKit.AddText(rect, name, size, alignment, UiKit.TextDark);
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.6f;
            label.fontSizeMax = size;
            return label;
        }
    }
}
