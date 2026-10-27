using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The character's HUD portrait button (bottom left, above the tutorial and request cards) and their card.</summary>
    internal static class CharacterUiBuilder
    {
        private static readonly Color Navy = new Color(0.08f, 0.22f, 0.42f);
        private static readonly Color Muted = new Color(0.35f, 0.33f, 0.3f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);

        public static void Build(Transform parent)
        {
            RectTransform openRect = UiKit.Rect("CharacterButton", parent, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(30f, 690f), new Vector2(130f, 130f));
            Button open = UiKit.AddButton(openRect, Navy, out _);
            RawImage openPortrait = Picture(openRect, "Portrait", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116f, 116f));

            RectTransform host = UiKit.Stretch("CharacterCard", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "Your character", new Vector2(920f, 1000f), out RectTransform window, out Button close);
            RectTransform portraitFrame = UiKit.Rect("PortraitFrame", window, Top, Top, Top, new Vector2(-230f, -160f), new Vector2(290f, 290f));
            UiKit.AddImage(portraitFrame, Navy, false);
            RawImage portrait = Picture(portraitFrame, "Portrait", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(274f, 274f));
            RawImage flag = StartScreenUiBuilder.Flag(window, "Flag", new Vector2(-120f, -380f), new Vector2(120f, 80f));

            TextMeshProUGUI name = Text(window, "Name", new Vector2(175f, -190f), new Vector2(430f, 90f), 66f, Navy, FontStyles.Bold, TextAlignmentOptions.Left);
            TextMeshProUGUI tagline = Text(window, "Tagline", new Vector2(175f, -290f), new Vector2(430f, 110f), 36f, UiKit.TextDark, FontStyles.Italic, TextAlignmentOptions.TopLeft);
            TextMeshProUGUI ability = Text(window, "Ability", new Vector2(0f, -500f), new Vector2(800f, 130f), 40f, UiKit.TextDark, FontStyles.Normal, TextAlignmentOptions.Center);
            TextMeshProUGUI work = Text(window, "Work", new Vector2(0f, -650f), new Vector2(800f, 70f), 40f, Navy, FontStyles.Bold, TextAlignmentOptions.Center);
            TextMeshProUGUI hint = Text(window, "Hint", new Vector2(0f, -730f), new Vector2(800f, 90f), 32f, Muted, FontStyles.Italic, TextAlignmentOptions.Center);

            RectTransform okRect = UiKit.Rect("OkButton", window, Top, Top, Top, new Vector2(0f, -850f), new Vector2(520f, 120f));
            Button ok = UiKit.AddButton(okRect, UiKit.ButtonColor, out _);
            UiKit.ChildText(okRect, "Label", "OK", 48f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var view = host.gameObject.AddComponent<CharacterCardView>();
            PrimitiveKit.SetReference(view, "openButton", open);
            PrimitiveKit.SetReference(view, "openPortrait", openPortrait);
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "portrait", portrait);
            PrimitiveKit.SetReference(view, "flag", flag);
            PrimitiveKit.SetReference(view, "nameLabel", name);
            PrimitiveKit.SetReference(view, "taglineLabel", tagline);
            PrimitiveKit.SetReference(view, "abilityLabel", ability);
            PrimitiveKit.SetReference(view, "workLabel", work);
            PrimitiveKit.SetReference(view, "hintLabel", hint);
            PrimitiveKit.SetReference(view, "closeButton", close);
            PrimitiveKit.SetReference(view, "okButton", ok);
            root.gameObject.SetActive(false);
        }

        /// <summary>A picture filled in at runtime (portraits); taps go to the button under it.</summary>
        internal static RawImage Picture(RectTransform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = UiKit.Rect(name, parent, anchor, anchor, anchor, position, size);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI Text(RectTransform window, string name, Vector2 position, Vector2 size, float fontSize, Color color,
            FontStyles style, TextAlignmentOptions alignment)
        {
            RectTransform rect = UiKit.Rect(name, window, Top, Top, Top, position, size);
            return UiKit.AddText(rect, string.Empty, fontSize, alignment, color, style);
        }
    }
}
