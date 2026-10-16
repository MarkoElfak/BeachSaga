using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Milestone 3 HUD pieces: tutorial banner and arrow, 2x boost button, settings, offline popup.</summary>
    internal static class MetaUiBuilder
    {
        private static readonly Color Window = new Color(1f, 0.97f, 0.9f);
        private static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color Boost = new Color(1f, 0.55f, 0.25f);
        private static readonly Color Secondary = new Color(0.55f, 0.8f, 0.95f);

        public static void BuildTutorial(Transform parent, Camera camera, CoinSpawner coins, BuildBarView buildBar)
        {
            RectTransform layer = UiKit.Stretch("Tutorial", parent);

            RectTransform banner = UiKit.Rect("Banner", layer, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 390f), new Vector2(960f, 150f));
            UiKit.AddImage(banner, new Color(0.1f, 0.25f, 0.4f, 0.9f), false);
            TextMeshProUGUI message = UiKit.ChildText(banner, "Message", "Tutorial", 44f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            message.margin = new Vector4(30f, 10f, 30f, 10f);

            // Arrow: a stem above a diamond, pivoted at its tip so it points straight at the target.
            RectTransform arrow = UiKit.Rect("Arrow", layer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(90f, 150f));
            RectTransform stem = UiKit.Rect("Stem", arrow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 55f), new Vector2(30f, 90f));
            UiKit.AddImage(stem, Boost, false);
            RectTransform head = UiKit.Rect("Head", arrow, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(64f, 64f));
            head.localRotation = Quaternion.Euler(0f, 0f, 45f);
            UiKit.AddImage(head, Boost, false);

            var view = layer.gameObject.AddComponent<TutorialView>();
            PrimitiveKit.SetReference(view, "banner", banner.gameObject);
            PrimitiveKit.SetReference(view, "message", message);
            PrimitiveKit.SetReference(view, "arrow", arrow);
            PrimitiveKit.SetReference(view, "layer", layer);
            PrimitiveKit.SetReference(view, "worldCamera", camera);
            PrimitiveKit.SetReference(view, "coinSpawner", coins);
            PrimitiveKit.SetReference(view, "buildBar", buildBar);
        }

        public static void BuildBoostButton(Transform parent)
        {
            RectTransform rect = UiKit.Rect("BoostButton", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(-30f, -150f), new Vector2(330f, 100f));
            Button button = UiKit.AddButton(rect, Boost, out _);
            TextMeshProUGUI label = UiKit.ChildText(rect, "Label", "2x income (ad)", 38f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            var view = rect.gameObject.AddComponent<BoostButtonView>();
            PrimitiveKit.SetReference(view, "button", button);
            PrimitiveKit.SetReference(view, "label", label);
        }

        public static void BuildSettings(Transform parent)
        {
            RectTransform openRect = UiKit.Rect("SettingsButton", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(-30f, -265f), new Vector2(200f, 90f));
            Button open = UiKit.AddButton(openRect, UiKit.PanelColor, out _);
            UiKit.ChildText(openRect, "Label", "Settings", 36f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            RectTransform host = UiKit.Stretch("SettingsPanel", parent);
            RectTransform root = Modal(host, "Settings", new Vector2(820f, 820f), out RectTransform window, out Button close);
            Button sound = Row(window, "SoundButton", 0, Secondary, out TextMeshProUGUI soundLabel);
            Button music = Row(window, "MusicButton", 1, Secondary, out TextMeshProUGUI musicLabel);
            Button removeAds = Row(window, "RemoveAdsButton", 2, UiKit.ButtonColor, out TextMeshProUGUI removeAdsLabel);

            var view = host.gameObject.AddComponent<SettingsPanelView>();
            PrimitiveKit.SetReference(view, "openButton", open);
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "soundButton", sound);
            PrimitiveKit.SetReference(view, "soundLabel", soundLabel);
            PrimitiveKit.SetReference(view, "musicButton", music);
            PrimitiveKit.SetReference(view, "musicLabel", musicLabel);
            PrimitiveKit.SetReference(view, "removeAdsButton", removeAds);
            PrimitiveKit.SetReference(view, "removeAdsLabel", removeAdsLabel);
            PrimitiveKit.SetReference(view, "closeButton", close);
            root.gameObject.SetActive(false);
        }

        public static void BuildOfflinePopup(Transform parent)
        {
            RectTransform host = UiKit.Stretch("OfflineEarnings", parent);
            RectTransform root = Modal(host, "Welcome back!", new Vector2(920f, 820f), out RectTransform window, out Button close);
            close.gameObject.SetActive(false); // the reward must be collected (single tap), not dismissed

            RectTransform bodyRect = UiKit.Rect("Body", window, new Vector2(0f, 0.45f), new Vector2(1f, 0.82f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-100f, 0f));
            TextMeshProUGUI body = UiKit.AddText(bodyRect, "You earned coins", 44f, TextAlignmentOptions.Center, UiKit.TextDark);
            Button collect = Row(window, "CollectButton", 2, Secondary, out TextMeshProUGUI collectLabel);
            collectLabel.text = "Collect";
            Button doubled = Row(window, "DoubleButton", 3, UiKit.ButtonColor, out TextMeshProUGUI doubleLabel);

            var view = host.gameObject.AddComponent<OfflineEarningsPopupView>();
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "body", body);
            PrimitiveKit.SetReference(view, "collectButton", collect);
            PrimitiveKit.SetReference(view, "doubleButton", doubled);
            PrimitiveKit.SetReference(view, "doubleLabel", doubleLabel);
            root.gameObject.SetActive(false);
        }

        // Dimmed full-screen blocker plus a centred window with a title and a close button.
        private static RectTransform Modal(Transform host, string title, Vector2 size, out RectTransform window, out Button close)
        {
            RectTransform root = UiKit.Stretch("Root", host);
            UiKit.AddImage(UiKit.Stretch("Backdrop", root), Dim);
            window = UiKit.Rect("Window", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            UiKit.AddImage(window, Window);
            RectTransform titleRect = UiKit.Rect("Title", window, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-160f, 100f));
            UiKit.AddText(titleRect, title, 60f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            RectTransform closeRect = UiKit.Rect("CloseButton", window, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(110f, 110f));
            close = UiKit.AddButton(closeRect, new Color(0.85f, 0.35f, 0.3f), out _);
            UiKit.ChildText(closeRect, "Label", "X", 56f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            return root;
        }

        // Wide button in a vertical stack below the title (index 0 is the top row).
        private static Button Row(RectTransform window, string name, int index, Color color, out TextMeshProUGUI label)
        {
            const float top = -190f, spacing = 150f;
            RectTransform rect = UiKit.Rect(name, window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, top - index * spacing), new Vector2(640f, 120f));
            Button button = UiKit.AddButton(rect, color, out _);
            label = UiKit.ChildText(rect, "Label", name, 46f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }
    }
}
