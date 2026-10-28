using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Milestone 2 HUD pieces: quest tracker, move-in request card, toast and the decoration tiles of the build bar.</summary>
    internal static class CommunityUiBuilder
    {
        private const string DecorButtonPath = ProjectPaths.Prefabs + "/UI/DecorButton.prefab";
        private static readonly Color CardColor = new Color(1f, 0.97f, 0.9f, 0.96f);

        public static void BuildQuestTracker(Transform parent)
        {
            RectTransform panel = UiKit.Rect("QuestTracker", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -150f), new Vector2(620f, 150f));
            UiKit.AddImage(panel, new Color(0f, 0f, 0f, 0.35f), false);
            RectTransform labelRect = UiKit.Rect("Label", panel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-30f, -16f));
            TextMeshProUGUI label = UiKit.AddText(labelRect, "", 30f, TextAlignmentOptions.MidlineLeft, Color.white);
            var view = panel.gameObject.AddComponent<QuestTrackerView>();
            PrimitiveKit.SetReference(view, "panel", panel.gameObject);
            PrimitiveKit.SetReference(view, "label", label);
        }

        public static void BuildNeighbourRequest(Transform parent)
        {
            RectTransform host = UiKit.Rect("NeighbourRequest", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 400f), new Vector2(960f, 260f));
            RectTransform root = UiKit.Stretch("Root", host);
            UiKit.AddImage(root, CardColor);
            RectTransform textRect = UiKit.Rect("Message", root, new Vector2(0f, 0.45f), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-60f, -20f));
            TextMeshProUGUI message = UiKit.AddText(textRect, "", 42f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            Button accept = CardButton(root, "Accept", "Welcome!", new Vector2(0.27f, 0.22f), UiKit.ButtonColor);
            Button decline = CardButton(root, "Decline", "Not now", new Vector2(0.73f, 0.22f), new Color(0.85f, 0.85f, 0.85f));
            var view = host.gameObject.AddComponent<NeighbourRequestView>();
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "message", message);
            PrimitiveKit.SetReference(view, "acceptButton", accept);
            PrimitiveKit.SetReference(view, "declineButton", decline);
            root.gameObject.SetActive(false);
        }

        public static void BuildToast(Transform parent)
        {
            RectTransform rect = UiKit.Rect("Toast", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, -445f), new Vector2(780f, 110f));
            UiKit.AddImage(rect, new Color(0.1f, 0.25f, 0.35f, 0.85f), false);
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            TextMeshProUGUI label = UiKit.ChildText(rect, "Label", "", 40f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            var view = rect.gameObject.AddComponent<ToastView>();
            PrimitiveKit.SetReference(view, "group", group);
            PrimitiveKit.SetReference(view, "label", label);
        }

        private static Button CardButton(Transform parent, string name, string text, Vector2 anchor, Color color)
        {
            RectTransform rect = UiKit.Rect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 100f));
            Button button = UiKit.AddButton(rect, color, out _);
            UiKit.ChildText(rect, "Label", text, 40f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }

        /// <summary>A decoration in the build bar's Decor tab, the size of a product button: name, bonus and placed count, price.</summary>
        public static DecorButtonView BuildDecorTilePrefab()
        {
            var root = new GameObject("DecorButton", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(190f, 190f);
            Button button = UiKit.AddButton(rect, new Color(0.62f, 0.88f, 0.8f), out _);
            TextMeshProUGUI nameLabel = Line(rect, "Name", 0.66f, 1f, 30f, FontStyles.Bold);
            nameLabel.enableAutoSizing = true;
            nameLabel.fontSizeMin = 20f;
            nameLabel.fontSizeMax = 30f;
            nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI detail = Line(rect, "Detail", 0.38f, 0.66f, 26f, FontStyles.Normal);
            RectTransform priceRect = UiKit.Rect("Price", rect, new Vector2(0f, 0.06f), new Vector2(1f, 0.36f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, 0f));
            UiKit.AddImage(priceRect, UiKit.ButtonColor, false);
            TextMeshProUGUI price = UiKit.ChildText(priceRect, "Label", "0", 30f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var view = root.AddComponent<DecorButtonView>();
            PrimitiveKit.SetReference(view, "button", button);
            PrimitiveKit.SetReference(view, "nameLabel", nameLabel);
            PrimitiveKit.SetReference(view, "detailLabel", detail);
            PrimitiveKit.SetReference(view, "priceLabel", price);
            return PrimitiveKit.SavePrefab(root, DecorButtonPath).GetComponent<DecorButtonView>();
        }

        private static TextMeshProUGUI Line(RectTransform parent, string name, float yMin, float yMax, float size, FontStyles style)
        {
            RectTransform rect = UiKit.Rect(name, parent, new Vector2(0f, yMin), new Vector2(1f, yMax), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, 0f));
            return UiKit.AddText(rect, name, size, TextAlignmentOptions.Center, UiKit.TextDark, style);
        }
    }
}
