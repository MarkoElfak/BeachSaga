using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The start screen: game title over the sky, a greeting page (flag, name, Play) and the page that founds the
    /// village (name and culture cards) or renames it.
    /// </summary>
    internal static class StartScreenUiBuilder
    {
        private static readonly Color Sky = new Color(0.36f, 0.72f, 0.98f);
        private static readonly Color Navy = new Color(0.08f, 0.22f, 0.42f);
        private static readonly Color Card = new Color(1f, 0.97f, 0.9f);
        private static readonly Color ErrorRed = new Color(0.8f, 0.2f, 0.15f);
        private static readonly Color FlagFrame = new Color(0.25f, 0.25f, 0.28f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);

        public static StartScreenView Build(Transform parent)
        {
            RectTransform host = UiKit.Stretch("StartScreen", parent);
            RectTransform root = UiKit.Stretch("Root", host);
            UiKit.AddImage(root, Sky);
            Text(root, "GameTitle", "Beach Tycoon", -230f, 150f, 130f, Navy, FontStyles.Bold);
            Text(root, "Tagline", "Build your seaside village", -380f, 70f, 46f, Navy, FontStyles.Italic);

            RectTransform window = UiKit.Rect("Window", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(900f, 1180f));
            UiKit.AddImage(window, Card);
            MetaUiBuilder.AddModalWindow(root, window);

            var view = host.gameObject.AddComponent<StartScreenView>();
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            BuildTitlePage(window, view);
            BuildNamePage(window, view);
            root.gameObject.SetActive(false);
            return view;
        }

        private static void BuildTitlePage(RectTransform window, StartScreenView view)
        {
            RectTransform page = UiKit.Stretch("TitlePage", window);
            RawImage flag = Flag(page, "Flag", new Vector2(0f, -70f), new Vector2(300f, 200f));
            Text(page, "Welcome", "Welcome back to", -320f, 80f, 46f, UiKit.TextDark, FontStyles.Normal);
            TextMeshProUGUI village = Text(page, "VillageName", "Village", -400f, 130f, 84f, Navy, FontStyles.Bold);
            TextMeshProUGUI culture = Text(page, "CultureName", "Culture", -540f, 70f, 46f, UiKit.TextDark, FontStyles.Italic);
            Button play = WideButton(page, "PlayButton", -900f, UiKit.ButtonColor, out TextMeshProUGUI playLabel);
            playLabel.text = "Play";
            playLabel.fontSize = 64f;
            PrimitiveKit.SetReference(view, "titlePage", page.gameObject);
            PrimitiveKit.SetReference(view, "titleFlag", flag);
            PrimitiveKit.SetReference(view, "villageLabel", village);
            PrimitiveKit.SetReference(view, "cultureLabel", culture);
            PrimitiveKit.SetReference(view, "playButton", play);
        }

        private static void BuildNamePage(RectTransform window, StartScreenView view)
        {
            RectTransform page = UiKit.Stretch("NamePage", window);
            TextMeshProUGUI heading = Text(page, "Heading", "Found your village", -50f, 100f, 54f, UiKit.TextDark, FontStyles.Bold);
            heading.rectTransform.sizeDelta = new Vector2(-260f, 100f); // clear of the close button
            TextMeshProUGUI hint = Text(page, "Hint", "Hint", -150f, 60f, 34f, UiKit.TextDark, FontStyles.Italic);
            TMP_InputField input = NameInput(page);

            RectTransform ideaRect = UiKit.Rect("IdeaButton", page, Top, Top, Top, new Vector2(300f, -235f), new Vector2(150f, 130f));
            Button idea = UiKit.AddButton(ideaRect, MetaUiBuilder.Secondary, out _);
            UiKit.ChildText(ideaRect, "Label", "Idea", 42f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            CulturePickerView picker = BuildPicker(page);
            TextMeshProUGUI error = Text(page, "Error", string.Empty, -810f, 80f, 36f, ErrorRed, FontStyles.Bold);
            Button confirm = WideButton(page, "ConfirmButton", -900f, UiKit.ButtonColor, out TextMeshProUGUI confirmLabel);
            confirmLabel.text = "Found village";

            RectTransform closeRect = UiKit.Rect("CloseButton", page, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(110f, 110f));
            Button close = UiKit.AddButton(closeRect, new Color(0.85f, 0.35f, 0.3f), out _);
            UiKit.ChildText(closeRect, "Label", "X", 56f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            PrimitiveKit.SetReference(view, "namePage", page.gameObject);
            PrimitiveKit.SetReference(view, "heading", heading);
            PrimitiveKit.SetReference(view, "hint", hint);
            PrimitiveKit.SetReference(view, "nameInput", input);
            PrimitiveKit.SetReference(view, "ideaButton", idea);
            PrimitiveKit.SetReference(view, "picker", picker);
            PrimitiveKit.SetReference(view, "error", error);
            PrimitiveKit.SetReference(view, "confirmButton", confirm);
            PrimitiveKit.SetReference(view, "confirmLabel", confirmLabel);
            PrimitiveKit.SetReference(view, "closeButton", close);
            page.gameObject.SetActive(false);
        }

        // "Choose your culture" over a row of cards; the view makes one card per culture from the hidden template.
        private static CulturePickerView BuildPicker(RectTransform page)
        {
            RectTransform rect = UiKit.Rect("CulturePicker", page, Top, Top, Top, new Vector2(0f, -400f), new Vector2(820f, 390f));
            TextMeshProUGUI pickerLabel = Text(rect, "Label", "Choose your culture", 0f, 60f, 40f, Navy, FontStyles.Bold);
            RectTransform row = UiKit.Rect("Cards", rect, Top, Top, Top, new Vector2(0f, -80f), new Vector2(820f, 300f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform cardRect = UiKit.Rect("CardTemplate", row, Top, Top, Top, Vector2.zero, new Vector2(250f, 300f));
            Button button = UiKit.AddButton(cardRect, Color.white, out Image background);
            RawImage flag = Flag(cardRect, "Flag", new Vector2(0f, -30f), new Vector2(180f, 120f));
            RectTransform labelRect = UiKit.Rect("Name", cardRect, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(-20f, 90f));
            TextMeshProUGUI label = UiKit.AddText(labelRect, "Culture", 44f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var card = cardRect.gameObject.AddComponent<CultureCardView>();
            PrimitiveKit.SetReference(card, "button", button);
            PrimitiveKit.SetReference(card, "background", background);
            PrimitiveKit.SetReference(card, "flag", flag);
            PrimitiveKit.SetReference(card, "label", label);
            cardRect.gameObject.SetActive(false);

            var picker = rect.gameObject.AddComponent<CulturePickerView>();
            PrimitiveKit.SetReference(picker, "cardTemplate", card);
            PrimitiveKit.SetReference(picker, "cardRow", row);
            PrimitiveKit.SetReference(picker, "label", pickerLabel.gameObject);
            return picker;
        }

        /// <summary>A flag picture in a thin dark frame (the image is set at runtime).</summary>
        internal static RawImage Flag(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform frame = UiKit.Rect(name + "Frame", parent, Top, Top, Top, position, size + new Vector2(8f, 8f));
            UiKit.AddImage(frame, FlagFrame, false);
            RectTransform rect = UiKit.Rect(name, frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }

        // A single-line text field: white box, clipped text area, grey placeholder.
        private static TMP_InputField NameInput(RectTransform page)
        {
            RectTransform rect = UiKit.Rect("NameInput", page, Top, Top, Top, new Vector2(-80f, -235f), new Vector2(560f, 130f));
            Image background = UiKit.AddImage(rect, Color.white);
            RectTransform area = UiKit.Rect("TextArea", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-40f, -20f));
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI placeholder = UiKit.AddText(UiKit.Stretch("Placeholder", area), "Village name", 50f, TextAlignmentOptions.MidlineLeft, new Color(0.6f, 0.6f, 0.6f), FontStyles.Italic);
            TextMeshProUGUI text = UiKit.AddText(UiKit.Stretch("Text", area), string.Empty, 50f, TextAlignmentOptions.MidlineLeft, UiKit.TextDark);
            placeholder.textWrappingMode = TextWrappingModes.NoWrap;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            var input = rect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = background;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = text.font;
            input.pointSize = 50f;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.richText = false;
            return input;
        }

        private static Button WideButton(RectTransform page, string name, float y, Color color, out TextMeshProUGUI label)
        {
            RectTransform rect = UiKit.Rect(name, page, Top, Top, Top, new Vector2(0f, y), new Vector2(640f, 150f));
            Button button = UiKit.AddButton(rect, color, out _);
            label = UiKit.ChildText(rect, "Label", name, 52f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }

        private static TextMeshProUGUI Text(RectTransform parent, string name, string text, float y, float height, float size,
            Color color, FontStyles style)
        {
            RectTransform rect = UiKit.Rect(name, parent, Top, Top, Top, new Vector2(0f, y), new Vector2(-80f, height));
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            return UiKit.AddText(rect, text, size, TextAlignmentOptions.Center, color, style);
        }
    }
}
