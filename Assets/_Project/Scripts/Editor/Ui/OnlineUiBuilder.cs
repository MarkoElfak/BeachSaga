using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Visiting UI: the Beach HUD's Visit button and island list, and the Island scene HUD.</summary>
    internal static class OnlineUiBuilder
    {
        private const int VisitRows = 4; // own snapshot + three demo friends
        private static readonly Color Visit = new Color(0.45f, 0.85f, 0.8f);
        private static readonly Color Like = new Color(0.98f, 0.6f, 0.7f);
        private static readonly Color Gift = new Color(0.75f, 0.65f, 0.98f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

        public static void BuildVisitPanel(Transform parent)
        {
            RectTransform openRect = UiKit.Rect("VisitButton", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(-30f, -475f), new Vector2(200f, 90f));
            Button open = UiKit.AddButton(openRect, Visit, out _);
            UiKit.ChildText(openRect, "Label", "Visit", 36f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform host = UiKit.Stretch("VisitPanel", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "Visit islands", new Vector2(920f, 1080f), out RectTransform window, out Button close);
            RectTransform bodyRect = UiKit.Rect("Body", window, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(-100f, 130f));
            TextMeshProUGUI body = UiKit.AddText(bodyRect, "Visit a friend's beach", 38f, TextAlignmentOptions.Center, UiKit.TextDark);

            var buttons = new Button[VisitRows];
            var labels = new TextMeshProUGUI[VisitRows];
            for (int i = 0; i < VisitRows; i++)
            {
                RectTransform rect = UiKit.Rect($"Row{i}", window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -310f - i * 170f), new Vector2(780f, 140f));
                buttons[i] = UiKit.AddButton(rect, i == 0 ? UiKit.ButtonColor : Visit, out _);
                labels[i] = UiKit.ChildText(rect, "Label", "Island", 40f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
                labels[i].margin = new Vector4(20f, 0f, 20f, 0f);
            }

            var view = host.gameObject.AddComponent<VisitPanelView>();
            PrimitiveKit.SetReference(view, "openButton", open);
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReferences(view, "rowButtons", buttons);
            PrimitiveKit.SetReferences(view, "rowLabels", labels);
            PrimitiveKit.SetReference(view, "body", body);
            PrimitiveKit.SetReference(view, "closeButton", close);
            root.gameObject.SetActive(false);
        }

        public static void BuildIslandHud()
        {
            RectTransform safe = HudBuilder.CreateCanvas("IslandHUD");
            HudBuilder.BuildCoinCounter(safe);

            RectTransform banner = UiKit.Rect("Banner", safe, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(-60f, 270f));
            UiKit.AddImage(banner, UiKit.PanelColor, false);
            TextMeshProUGUI title = Line(banner, "Title", 0.62f, 1f, 56f, Color.white, FontStyles.Bold);
            TextMeshProUGUI details = Line(banner, "Details", 0.34f, 0.62f, 38f, Color.white, FontStyles.Normal);
            TextMeshProUGUI bonus = Line(banner, "Bonus", 0.04f, 0.34f, 40f, Gold, FontStyles.Bold);

            Button like = BottomButton(safe, "LikeButton", -340f, Like, out TextMeshProUGUI likeLabel);
            Button gift = BottomButton(safe, "GiftButton", 0f, Gift, out TextMeshProUGUI giftLabel);
            Button home = BottomButton(safe, "HomeButton", 340f, UiKit.ButtonColor, out TextMeshProUGUI homeLabel);
            homeLabel.text = "Home";

            var view = safe.gameObject.AddComponent<IslandHudView>();
            PrimitiveKit.SetReference(view, "title", title);
            PrimitiveKit.SetReference(view, "details", details);
            PrimitiveKit.SetReference(view, "bonusLabel", bonus);
            PrimitiveKit.SetReference(view, "likeButton", like);
            PrimitiveKit.SetReference(view, "likeLabel", likeLabel);
            PrimitiveKit.SetReference(view, "giftButton", gift);
            PrimitiveKit.SetReference(view, "giftLabel", giftLabel);
            PrimitiveKit.SetReference(view, "homeButton", home);
        }

        private static TextMeshProUGUI Line(RectTransform parent, string name, float yMin, float yMax, float size, Color color, FontStyles style)
        {
            RectTransform rect = UiKit.Rect(name, parent, new Vector2(0f, yMin), new Vector2(1f, yMax), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, 0f));
            return UiKit.AddText(rect, name, size, TextAlignmentOptions.Center, color, style);
        }

        private static Button BottomButton(Transform parent, string name, float x, Color color, out TextMeshProUGUI label)
        {
            RectTransform rect = UiKit.Rect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 60f), new Vector2(310f, 150f));
            Button button = UiKit.AddButton(rect, color, out _);
            label = UiKit.ChildText(rect, "Label", name, 44f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }
    }
}
