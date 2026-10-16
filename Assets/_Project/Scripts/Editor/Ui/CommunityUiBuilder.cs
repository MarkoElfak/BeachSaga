using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Milestone 2 HUD pieces: quest tracker, move-in request card, toast and the decorations shop.</summary>
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
            RectTransform rect = UiKit.Rect("Toast", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(900f, 110f));
            UiKit.AddImage(rect, new Color(0.1f, 0.25f, 0.35f, 0.85f), false);
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            TextMeshProUGUI label = UiKit.ChildText(rect, "Label", "", 40f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            var view = rect.gameObject.AddComponent<ToastView>();
            PrimitiveKit.SetReference(view, "group", group);
            PrimitiveKit.SetReference(view, "label", label);
        }

        public static DecorPanelView BuildDecorPanel(Transform parent)
        {
            DecorButtonView buttonPrefab = BuildDecorButtonPrefab();
            RectTransform host = UiKit.Stretch("DecorPanel", parent);
            RectTransform root = UiKit.Stretch("Root", host);
            RectTransform backdropRect = UiKit.Stretch("Backdrop", root);
            Button backdrop = UiKit.AddButton(backdropRect, new Color(0f, 0f, 0f, 0.55f), out _);
            backdrop.transition = Selectable.Transition.None;

            RectTransform window = UiKit.Rect("Window", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 1180f));
            UiKit.AddImage(window, CardColor);
            RectTransform titleRect = UiKit.Rect("Title", window, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(-160f, 100f));
            UiKit.AddText(titleRect, "Beach decorations", 56f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform list = UiKit.Rect("List", window, Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(-60f, -200f));
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            RectTransform closeRect = UiKit.Rect("CloseButton", window, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(110f, 110f));
            Button close = UiKit.AddButton(closeRect, new Color(0.85f, 0.35f, 0.3f), out _);
            UiKit.ChildText(closeRect, "Label", "X", 56f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            MetaUiBuilder.AddModalWindow(root, window);
            var view = host.gameObject.AddComponent<DecorPanelView>();
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "buttonPrefab", buttonPrefab);
            PrimitiveKit.SetReference(view, "list", list);
            PrimitiveKit.SetReference(view, "closeButton", close);
            PrimitiveKit.SetReference(view, "backdropButton", backdrop);
            root.gameObject.SetActive(false);
            return view;
        }

        private static Button CardButton(Transform parent, string name, string text, Vector2 anchor, Color color)
        {
            RectTransform rect = UiKit.Rect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 100f));
            Button button = UiKit.AddButton(rect, color, out _);
            UiKit.ChildText(rect, "Label", text, 40f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }

        private static DecorButtonView BuildDecorButtonPrefab()
        {
            var root = new GameObject("DecorButton", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(800f, 170f);
            Button button = UiKit.AddButton(rect, Color.white, out _);
            RectTransform nameRect = UiKit.Rect("Name", rect, new Vector2(0f, 0.5f), new Vector2(0.7f, 1f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            TextMeshProUGUI nameLabel = UiKit.AddText(nameRect, "Name", 44f, TextAlignmentOptions.MidlineLeft, UiKit.TextDark, FontStyles.Bold);
            RectTransform detailRect = UiKit.Rect("Detail", rect, new Vector2(0f, 0f), new Vector2(0.7f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(-30f, 0f));
            TextMeshProUGUI detail = UiKit.AddText(detailRect, "Detail", 30f, TextAlignmentOptions.MidlineLeft, UiKit.TextDark);
            RectTransform priceRect = UiKit.Rect("Price", rect, new Vector2(0.7f, 0.15f), new Vector2(1f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-30f, 0f));
            UiKit.AddImage(priceRect, UiKit.ButtonColor, false);
            TextMeshProUGUI price = UiKit.ChildText(priceRect, "Label", "0", 42f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var view = root.AddComponent<DecorButtonView>();
            PrimitiveKit.SetReference(view, "button", button);
            PrimitiveKit.SetReference(view, "nameLabel", nameLabel);
            PrimitiveKit.SetReference(view, "detailLabel", detail);
            PrimitiveKit.SetReference(view, "priceLabel", price);
            return PrimitiveKit.SavePrefab(root, DecorButtonPath).GetComponent<DecorButtonView>();
        }
    }
}
