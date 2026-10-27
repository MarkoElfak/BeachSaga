using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The staff HUD button (right column, under Trophies) with its job-seeker badge, and the staff list.</summary>
    internal static class StaffUiBuilder
    {
        private const int Rows = 10;
        private static readonly Color Staff = new Color(0.6f, 0.85f, 0.95f);
        private static readonly Color Navy = new Color(0.08f, 0.22f, 0.42f);
        private static readonly Color Dismiss = new Color(0.92f, 0.6f, 0.55f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);

        public static void Build(Transform parent)
        {
            RectTransform openRect = UiKit.Rect("StaffButton", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(-30f, -685f), new Vector2(200f, 90f));
            Button open = UiKit.AddButton(openRect, Staff, out _);
            TextMeshProUGUI openLabel = UiKit.ChildText(openRect, "Label", "Staff 0/0", 34f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            RectTransform badgeRect = UiKit.Rect("Badge", openRect, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), new Vector2(48f, 48f));
            UiKit.AddImage(badgeRect, new Color(1f, 0.75f, 0.15f), false);
            UiKit.ChildText(badgeRect, "Label", "!", 34f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform host = UiKit.Stretch("StaffPanel", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "Staff", new Vector2(940f, 1660f), out RectTransform window, out Button close);
            TextMeshProUGUI summary = Text(window, "Summary", -140f, 100f, 38f, Navy, FontStyles.Bold);
            TextMeshProUGUI seekers = Text(window, "Seekers", -245f, 90f, 32f, UiKit.TextDark, FontStyles.Normal);
            seekers.rectTransform.sizeDelta = new Vector2(-330f, 90f);
            seekers.rectTransform.anchoredPosition = new Vector2(-120f, -245f);
            RectTransform findRect = UiKit.Rect("FindButton", window, Vector2.one, Vector2.one, Vector2.one, new Vector2(-40f, -250f), new Vector2(230f, 80f));
            Button find = UiKit.AddButton(findRect, UiKit.ButtonColor, out _);
            UiKit.ChildText(findRect, "Label", "Show me", 34f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var rows = new Object[Rows];
            for (int i = 0; i < Rows; i++) rows[i] = Row(window, i);
            TextMeshProUGUI empty = Text(window, "Empty", -420f, 160f, 36f, new Color(0.35f, 0.33f, 0.3f), FontStyles.Italic);
            empty.text = "Nobody hired yet. Job seekers wear a ! over their head.";

            var view = host.gameObject.AddComponent<StaffPanelView>();
            PrimitiveKit.SetReference(view, "openButton", open);
            PrimitiveKit.SetReference(view, "openLabel", openLabel);
            PrimitiveKit.SetReference(view, "badge", badgeRect.gameObject);
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "closeButton", close);
            PrimitiveKit.SetReference(view, "summaryLabel", summary);
            PrimitiveKit.SetReference(view, "seekersLabel", seekers);
            PrimitiveKit.SetReference(view, "findButton", find);
            PrimitiveKit.SetReferences(view, "rows", rows);
            PrimitiveKit.SetReference(view, "emptyLabel", empty);
            root.gameObject.SetActive(false);
        }

        // Portrait, two lines of text, Move and Dismiss.
        private static StaffRowView Row(RectTransform window, int index)
        {
            RectTransform rect = UiKit.Rect($"Row{index}", window, Top, Top, Top, new Vector2(0f, -350f - index * 125f), new Vector2(880f, 112f));
            UiKit.AddImage(rect, new Color(0f, 0f, 0f, 0.06f), false);
            RectTransform frame = UiKit.Rect("Frame", rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(100f, 100f));
            UiKit.AddImage(frame, Navy, false);
            RawImage portrait = CharacterUiBuilder.Picture(frame, "Portrait", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 92f));
            RectTransform labelRect = UiKit.Rect("Label", rect, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), new Vector2(122f, 0f), new Vector2(-470f, 0f));
            labelRect.anchorMax = new Vector2(1f, 1f);
            TextMeshProUGUI label = UiKit.AddText(labelRect, "Name", 32f, TextAlignmentOptions.MidlineLeft, UiKit.TextDark);
            Button move = RowButton(rect, "MoveButton", "Move", new Vector2(-185f, 0f), UiKit.ButtonColor, out _);
            Button dismiss = RowButton(rect, "DismissButton", "Dismiss", new Vector2(-10f, 0f), Dismiss, out TextMeshProUGUI dismissLabel);

            var view = rect.gameObject.AddComponent<StaffRowView>();
            PrimitiveKit.SetReference(view, "portrait", portrait);
            PrimitiveKit.SetReference(view, "label", label);
            PrimitiveKit.SetReference(view, "moveButton", move);
            PrimitiveKit.SetReference(view, "dismissButton", dismiss);
            PrimitiveKit.SetReference(view, "dismissLabel", dismissLabel);
            return view;
        }

        private static Button RowButton(RectTransform row, string name, string text, Vector2 position, Color color, out TextMeshProUGUI label)
        {
            RectTransform rect = UiKit.Rect(name, row, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), position, new Vector2(165f, 86f));
            Button button = UiKit.AddButton(rect, color, out _);
            label = UiKit.ChildText(rect, "Label", text, 32f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }

        internal static TextMeshProUGUI Text(RectTransform window, string name, float y, float height, float size, Color color, FontStyles style)
        {
            RectTransform rect = UiKit.Rect(name, window, new Vector2(0f, 1f), Vector2.one, Top, new Vector2(0f, y), new Vector2(-80f, height));
            return UiKit.AddText(rect, string.Empty, size, TextAlignmentOptions.Center, color, style);
        }
    }
}
