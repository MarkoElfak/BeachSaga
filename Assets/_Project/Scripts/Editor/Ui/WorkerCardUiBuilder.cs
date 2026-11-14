using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The worker card: portrait and flag, name, country, level and wage, five skill rows of stars, stand buttons and Dismiss.</summary>
    internal static class WorkerCardUiBuilder
    {
        private const int StandButtons = 12;
        private const int Columns = 3;
        private const int Stars = 5;
        private static readonly Color Navy = new Color(0.08f, 0.22f, 0.42f);
        private static readonly Color Muted = new Color(0.35f, 0.33f, 0.3f);
        private static readonly Color Stand = new Color(0.62f, 0.86f, 0.62f);
        private static readonly Color Dismiss = new Color(0.92f, 0.6f, 0.55f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);

        public static void Build(Transform parent)
        {
            RectTransform host = UiKit.Stretch("WorkerCard", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "Job seeker", new Vector2(920f, 1580f), out RectTransform window, out Button close);
            TMP_Text title = window.Find("Title").GetComponent<TMP_Text>();
            RectTransform frame = UiKit.Rect("PortraitFrame", window, Top, Top, Top, new Vector2(-250f, -150f), new Vector2(260f, 260f));
            UiKit.AddImage(frame, Navy, false);
            RawImage portrait = CharacterUiBuilder.Picture(frame, "Portrait", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(246f, 246f));
            RawImage flag = StartScreenUiBuilder.Flag(window, "Flag", new Vector2(-150f, -375f), new Vector2(96f, 64f));

            TextMeshProUGUI name = Text(window, "Name", new Vector2(155f, -150f), new Vector2(490f, 90f), 62f, Navy, FontStyles.Bold, TextAlignmentOptions.Left);
            TextMeshProUGUI info = Text(window, "Info", new Vector2(155f, -245f), new Vector2(490f, 170f), 34f, UiKit.TextDark, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            TextMeshProUGUI status = Text(window, "Status", new Vector2(0f, -440f), new Vector2(840f, 64f), 38f, Navy, FontStyles.Bold, TextAlignmentOptions.Center);

            var skills = new Object[5];
            var stars = new Object[skills.Length * Stars];
            for (int row = 0; row < skills.Length; row++)
            {
                float y = -530f - row * 68f;
                skills[row] = Text(window, $"Skill{row}", new Vector2(-170f, y), new Vector2(340f, 60f), 38f, UiKit.TextDark, FontStyles.Normal, TextAlignmentOptions.Left);
                for (int i = 0; i < Stars; i++)
                {
                    RectTransform star = UiKit.Rect($"Star{row}_{i}", window, Top, Top, Top, new Vector2(60f + i * 62f, y - 10f), new Vector2(40f, 40f));
                    star.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    stars[row * Stars + i] = UiKit.AddImage(star, Color.white, false);
                }
            }

            TextMeshProUGUI hint = Text(window, "Hint", new Vector2(0f, -890f), new Vector2(840f, 60f), 34f, Muted, FontStyles.Italic, TextAlignmentOptions.Center);
            var buttons = new Object[StandButtons];
            for (int i = 0; i < StandButtons; i++)
            {
                var at = new Vector2((i % Columns - 1) * 285f, -960f - (i / Columns) * 118f);
                buttons[i] = Button(window, $"Stand{i}", "Stand", at, new Vector2(272f, 106f), Stand, 32f, out _);
            }

            Button dismiss = Button(window, "DismissButton", "Let go", new Vector2(0f, -1450f), new Vector2(600f, 100f), Dismiss, 36f, out TextMeshProUGUI dismissLabel);

            var view = host.gameObject.AddComponent<WorkerCardView>();
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "titleLabel", title);
            PrimitiveKit.SetReference(view, "portrait", portrait);
            PrimitiveKit.SetReference(view, "flag", flag);
            PrimitiveKit.SetReference(view, "nameLabel", name);
            PrimitiveKit.SetReference(view, "infoLabel", info);
            PrimitiveKit.SetReference(view, "statusLabel", status);
            PrimitiveKit.SetReferences(view, "skillLabels", skills);
            PrimitiveKit.SetReferences(view, "stars", stars);
            PrimitiveKit.SetReference(view, "hintLabel", hint);
            PrimitiveKit.SetReferences(view, "standButtons", buttons);
            PrimitiveKit.SetReference(view, "dismissButton", dismiss);
            PrimitiveKit.SetReference(view, "dismissLabel", dismissLabel);
            PrimitiveKit.SetReference(view, "closeButton", close);
            root.gameObject.SetActive(false);
        }

        private static Button Button(RectTransform window, string name, string text, Vector2 position, Vector2 size, Color color, float fontSize,
            out TextMeshProUGUI label)
        {
            RectTransform rect = UiKit.Rect(name, window, Top, Top, Top, position, size);
            Button button = UiKit.AddButton(rect, color, out _);
            label = UiKit.ChildText(rect, "Label", text, fontSize, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            label.lineSpacing = -10f;
            return button;
        }

        private static TextMeshProUGUI Text(RectTransform window, string name, Vector2 position, Vector2 size, float fontSize, Color color,
            FontStyles style, TextAlignmentOptions alignment)
        {
            RectTransform rect = UiKit.Rect(name, window, Top, Top, Top, position, size);
            return UiKit.AddText(rect, string.Empty, fontSize, alignment, color, style);
        }
    }
}
