using BeachTycoon.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The HUD Settings button and window: sound and music sliders, vibration and Remove ads.</summary>
    internal static class SettingsUiBuilder
    {
        private static readonly Color Track = new Color(0.82f, 0.78f, 0.7f);
        private static readonly Color Fill = new Color(0.35f, 0.7f, 0.95f);

        public static void Build(Transform parent)
        {
            RectTransform openRect = UiKit.Rect("SettingsButton", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(-30f, -265f), new Vector2(200f, 90f));
            Button open = UiKit.AddButton(openRect, UiKit.PanelColor, out _);
            UiKit.ChildText(openRect, "Label", "Settings", 36f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            RectTransform host = UiKit.Stretch("SettingsPanel", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "Settings", new Vector2(820f, 960f), out RectTransform window, out Button close);
            Slider sound = VolumeSlider(window, "Sound", -170f, out TextMeshProUGUI soundLabel);
            Slider music = VolumeSlider(window, "Music", -360f, out TextMeshProUGUI musicLabel);
            Button vibration = MetaUiBuilder.Row(window, "VibrationButton", 3, MetaUiBuilder.Secondary, out TextMeshProUGUI vibrationLabel);
            Button removeAds = MetaUiBuilder.Row(window, "RemoveAdsButton", 4, UiKit.ButtonColor, out TextMeshProUGUI removeAdsLabel);

            var view = host.gameObject.AddComponent<SettingsPanelView>();
            PrimitiveKit.SetReference(view, "openButton", open);
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "soundSlider", sound);
            PrimitiveKit.SetReference(view, "soundLabel", soundLabel);
            PrimitiveKit.SetReference(view, "musicSlider", music);
            PrimitiveKit.SetReference(view, "musicLabel", musicLabel);
            PrimitiveKit.SetReference(view, "vibrationButton", vibration);
            PrimitiveKit.SetReference(view, "vibrationLabel", vibrationLabel);
            PrimitiveKit.SetReference(view, "removeAdsButton", removeAds);
            PrimitiveKit.SetReference(view, "removeAdsLabel", removeAdsLabel);
            PrimitiveKit.SetReference(view, "closeButton", close);
            root.gameObject.SetActive(false);
        }

        // A label ("Sound: 80%") above a 0..1 slider with a round handle.
        private static Slider VolumeSlider(RectTransform window, string name, float y, out TextMeshProUGUI label)
        {
            var top = new Vector2(0.5f, 1f);
            RectTransform labelRect = UiKit.Rect(name + "Label", window, top, top, top, new Vector2(0f, y), new Vector2(640f, 60f));
            label = UiKit.AddText(labelRect, name, 44f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform rect = UiKit.Rect(name + "Slider", window, top, top, top, new Vector2(0f, y - 80f), new Vector2(600f, 70f));
            RectTransform track = UiKit.Rect("Track", rect, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.AddImage(track, Track);
            RectTransform fillArea = UiKit.Rect("FillArea", rect, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RectTransform fill = UiKit.Rect("Fill", fillArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.AddImage(fill, Fill, false);
            RectTransform handleArea = UiKit.Rect("HandleArea", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-70f, 0f));
            RectTransform handle = UiKit.Rect("Handle", handleArea, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 0f));
            Image knob = UiKit.AddImage(handle, Color.white);
            knob.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            var slider = rect.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }
    }
}
