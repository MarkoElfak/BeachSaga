using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The start screen: game title over the sky, a greeting page with Play and the village name page.</summary>
    internal static class StartScreenUiBuilder
    {
        private static readonly Color Sky = new Color(0.36f, 0.72f, 0.98f);
        private static readonly Color Navy = new Color(0.08f, 0.22f, 0.42f);
        private static readonly Color Card = new Color(1f, 0.97f, 0.9f);
        private static readonly Color ErrorRed = new Color(0.8f, 0.2f, 0.15f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);

        public static StartScreenView Build(Transform parent)
        {
            RectTransform host = UiKit.Stretch("StartScreen", parent);
            RectTransform root = UiKit.Stretch("Root", host);
            UiKit.AddImage(root, Sky);
            Text(root, "GameTitle", "Beach Tycoon", -230f, 150f, 130f, Navy, FontStyles.Bold);
            Text(root, "Tagline", "Build your seaside village", -380f, 70f, 46f, Navy, FontStyles.Italic);

            RectTransform window = UiKit.Rect("Window", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(900f, 820f));
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
            Text(page, "Welcome", "Welcome back to", -120f, 80f, 46f, UiKit.TextDark, FontStyles.Normal);
            TextMeshProUGUI village = Text(page, "VillageName", "Village", -220f, 130f, 84f, Navy, FontStyles.Bold);
            Button play = WideButton(page, "PlayButton", -560f, UiKit.ButtonColor, out TextMeshProUGUI playLabel);
            playLabel.text = "Play";
            playLabel.fontSize = 64f;
            PrimitiveKit.SetReference(view, "titlePage", page.gameObject);
            PrimitiveKit.SetReference(view, "villageLabel", village);
            PrimitiveKit.SetReference(view, "playButton", play);
        }

        private static void BuildNamePage(RectTransform window, StartScreenView view)
        {
            RectTransform page = UiKit.Stretch("NamePage", window);
            TextMeshProUGUI heading = Text(page, "Heading", "Name your village", -50f, 100f, 54f, UiKit.TextDark, FontStyles.Bold);
            heading.rectTransform.sizeDelta = new Vector2(-260f, 100f); // clear of the close button
            TextMeshProUGUI hint = Text(page, "Hint", "You can rename it later in Settings.", -160f, 60f, 36f, UiKit.TextDark, FontStyles.Italic);
            TMP_InputField input = NameInput(page);

            RectTransform ideaRect = UiKit.Rect("IdeaButton", page, Top, Top, Top, new Vector2(300f, -255f), new Vector2(150f, 130f));
            Button idea = UiKit.AddButton(ideaRect, MetaUiBuilder.Secondary, out _);
            UiKit.ChildText(ideaRect, "Label", "Idea", 42f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            TextMeshProUGUI error = Text(page, "Error", string.Empty, -410f, 90f, 36f, ErrorRed, FontStyles.Bold);
            Button confirm = WideButton(page, "ConfirmButton", -560f, UiKit.ButtonColor, out TextMeshProUGUI confirmLabel);
            confirmLabel.text = "Found village";

            RectTransform closeRect = UiKit.Rect("CloseButton", page, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -24f), new Vector2(110f, 110f));
            Button close = UiKit.AddButton(closeRect, new Color(0.85f, 0.35f, 0.3f), out _);
            UiKit.ChildText(closeRect, "Label", "X", 56f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            PrimitiveKit.SetReference(view, "namePage", page.gameObject);
            PrimitiveKit.SetReference(view, "heading", heading);
            PrimitiveKit.SetReference(view, "hint", hint);
            PrimitiveKit.SetReference(view, "nameInput", input);
            PrimitiveKit.SetReference(view, "ideaButton", idea);
            PrimitiveKit.SetReference(view, "error", error);
            PrimitiveKit.SetReference(view, "confirmButton", confirm);
            PrimitiveKit.SetReference(view, "confirmLabel", confirmLabel);
            PrimitiveKit.SetReference(view, "closeButton", close);
            page.gameObject.SetActive(false);
        }

        // A single-line text field: white box, clipped text area, grey placeholder.
        private static TMP_InputField NameInput(RectTransform page)
        {
            RectTransform rect = UiKit.Rect("NameInput", page, Top, Top, Top, new Vector2(-80f, -255f), new Vector2(560f, 130f));
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
