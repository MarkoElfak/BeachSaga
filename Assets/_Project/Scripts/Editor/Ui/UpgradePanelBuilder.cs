using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Builds the modal upgrade panel: dimmed backdrop, window, title, details, upgrade, hire and close buttons.</summary>
    internal static class UpgradePanelBuilder
    {
        public static void Build(Transform parent)
        {
            RectTransform host = UiKit.Stretch("UpgradePanel", parent);
            RectTransform root = UiKit.Stretch("Root", host);

            RectTransform backdropRect = UiKit.Stretch("Backdrop", root);
            Button backdrop = UiKit.AddButton(backdropRect, new Color(0f, 0f, 0f, 0.55f), out Image backdropImage);
            backdrop.transition = Selectable.Transition.None;
            backdropImage.raycastTarget = true;

            RectTransform window = UiKit.Rect("Window", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 900f));
            UiKit.AddImage(window, new Color(1f, 0.97f, 0.9f));

            RectTransform titleRect = UiKit.Rect("Title", window, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-160f, 110f));
            TextMeshProUGUI title = UiKit.AddText(titleRect, "Upgrade", 60f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform bodyRect = UiKit.Rect("Body", window, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(-120f, -500f));
            TextMeshProUGUI body = UiKit.AddText(bodyRect, "Details", 42f, TextAlignmentOptions.TopLeft, UiKit.TextDark);

            RectTransform upgradeRect = UiKit.Rect("UpgradeButton", window, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(620f, 140f));
            Button upgrade = UiKit.AddButton(upgradeRect, UiKit.ButtonColor, out _);
            TextMeshProUGUI upgradeLabel = UiKit.ChildText(upgradeRect, "Label", "Upgrade", 54f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform hireRect = UiKit.Rect("HireButton", window, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(620f, 120f));
            Button hire = UiKit.AddButton(hireRect, new Color(0.55f, 0.8f, 0.95f), out _);
            TextMeshProUGUI hireLabel = UiKit.ChildText(hireRect, "Label", "Staff", 46f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform closeRect = UiKit.Rect("CloseButton", window, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(110f, 110f));
            Button close = UiKit.AddButton(closeRect, new Color(0.85f, 0.35f, 0.3f), out _);
            UiKit.ChildText(closeRect, "Label", "X", 56f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            MetaUiBuilder.AddModalWindow(root, window);
            var view = host.gameObject.AddComponent<UpgradePanelView>();
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "title", title);
            PrimitiveKit.SetReference(view, "body", body);
            PrimitiveKit.SetReference(view, "upgradeButton", upgrade);
            PrimitiveKit.SetReference(view, "upgradeLabel", upgradeLabel);
            PrimitiveKit.SetReference(view, "hireButton", hire);
            PrimitiveKit.SetReference(view, "hireLabel", hireLabel);
            PrimitiveKit.SetReference(view, "closeButton", close);
            PrimitiveKit.SetReference(view, "backdropButton", backdrop);
            root.gameObject.SetActive(false);
        }
    }
}
