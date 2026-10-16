using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Achievements UI: the HUD trophy button with its badge, the scrollable panel and the row prefab.</summary>
    internal static class AchievementUiBuilder
    {
        private const string RowPrefabPath = ProjectPaths.Prefabs + "/UI/AchievementRow.prefab";
        private static readonly Color Trophy = new Color(1f, 0.85f, 0.3f);
        private static readonly Color Claim = new Color(0.45f, 0.8f, 0.45f);
        private static readonly Color BarBack = new Color(0f, 0f, 0f, 0.15f);
        private static readonly Color BarFill = new Color(0.3f, 0.7f, 0.95f);

        public static void Build(Transform parent)
        {
            AchievementRowView rowPrefab = BuildRowPrefab();
            RectTransform openRect = UiKit.Rect("TrophyButton", parent, Vector2.one, Vector2.one, Vector2.one, new Vector2(-30f, -580f), new Vector2(200f, 90f));
            Button open = UiKit.AddButton(openRect, Trophy, out _);
            UiKit.ChildText(openRect, "Label", "Trophies", 34f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            RectTransform badgeRect = UiKit.Rect("Badge", openRect, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f), new Vector2(48f, 48f));
            UiKit.AddImage(badgeRect, new Color(0.9f, 0.2f, 0.2f), false);
            TextMeshProUGUI badgeLabel = UiKit.ChildText(badgeRect, "Label", "1", 30f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            RectTransform host = UiKit.Stretch("AchievementPanel", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "Trophies", new Vector2(960f, 1500f), out RectTransform window, out Button close);
            RectTransform summaryRect = UiKit.Rect("Summary", window, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(-100f, 70f));
            TextMeshProUGUI summary = UiKit.AddText(summaryRect, "0 of 0 done", 38f, TextAlignmentOptions.Center, UiKit.TextDark);

            RectTransform scrollRect = UiKit.Rect("Scroll", window, Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -230f), new Vector2(-60f, -270f));
            UiKit.AddImage(scrollRect, new Color(0f, 0f, 0f, 0.04f)); // catches drags between rows
            scrollRect.gameObject.AddComponent<RectMask2D>();
            RectTransform content = UiKit.Rect("Content", scrollRect, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.viewport = scrollRect;
            scroll.content = content;
            scroll.scrollSensitivity = 40f;

            var view = host.gameObject.AddComponent<AchievementPanelView>();
            PrimitiveKit.SetReference(view, "openButton", open);
            PrimitiveKit.SetReference(view, "badge", badgeRect.gameObject);
            PrimitiveKit.SetReference(view, "badgeLabel", badgeLabel);
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "closeButton", close);
            PrimitiveKit.SetReference(view, "scroll", scroll);
            PrimitiveKit.SetReference(view, "content", content);
            PrimitiveKit.SetReference(view, "rowPrefab", rowPrefab);
            PrimitiveKit.SetReference(view, "summary", summary);
            root.gameObject.SetActive(false);
        }

        private static AchievementRowView BuildRowPrefab()
        {
            var root = new GameObject("AchievementRow", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(860f, 210f);
            Image background = UiKit.AddImage(rect, Color.white);

            TextMeshProUGUI title = Text(rect, "Title", new Vector2(0f, 0.64f), new Vector2(0.7f, 0.96f), 42f, FontStyles.Bold);
            TextMeshProUGUI description = Text(rect, "Description", new Vector2(0f, 0.38f), new Vector2(0.7f, 0.64f), 30f, FontStyles.Normal);

            RectTransform bar = UiKit.Rect("ProgressBar", rect, new Vector2(0f, 0.1f), new Vector2(0.7f, 0.33f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(-40f, 0f));
            UiKit.AddImage(bar, BarBack, false);
            RectTransform fill = UiKit.Rect("Fill", bar, Vector2.zero, new Vector2(0.4f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.AddImage(fill, BarFill, false);
            TextMeshProUGUI progress = UiKit.ChildText(bar, "Label", "0 / 0", 28f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            TextMeshProUGUI reward = Text(rect, "Reward", new Vector2(0.7f, 0.52f), new Vector2(1f, 0.96f), 32f, FontStyles.Bold);
            reward.alignment = TextAlignmentOptions.Center;
            RectTransform claimRect = UiKit.Rect("ClaimButton", rect, new Vector2(0.7f, 0.08f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 0f), new Vector2(-30f, 0f));
            Button claim = UiKit.AddButton(claimRect, Claim, out _);
            TextMeshProUGUI claimLabel = UiKit.ChildText(claimRect, "Label", "Claim", 38f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            var view = root.AddComponent<AchievementRowView>();
            PrimitiveKit.SetReference(view, "background", background);
            PrimitiveKit.SetReference(view, "titleLabel", title);
            PrimitiveKit.SetReference(view, "descriptionLabel", description);
            PrimitiveKit.SetReference(view, "progressFill", fill);
            PrimitiveKit.SetReference(view, "progressLabel", progress);
            PrimitiveKit.SetReference(view, "rewardLabel", reward);
            PrimitiveKit.SetReference(view, "claimButton", claim);
            PrimitiveKit.SetReference(view, "claimLabel", claimLabel);
            return PrimitiveKit.SavePrefab(root, RowPrefabPath).GetComponent<AchievementRowView>();
        }

        private static TextMeshProUGUI Text(RectTransform parent, string name, Vector2 min, Vector2 max, float size, FontStyles style)
        {
            RectTransform rect = UiKit.Rect(name, parent, min, max, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(-40f, 0f));
            return UiKit.AddText(rect, name, size, TextAlignmentOptions.MidlineLeft, UiKit.TextDark, style);
        }
    }
}
