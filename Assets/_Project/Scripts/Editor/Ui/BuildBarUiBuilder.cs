using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The bottom build bar (M20): Stands, Food, Services and Decor tabs over one row of buttons, and the upgrade button.</summary>
    internal static class BuildBarUiBuilder
    {
        // Tabs on top, the tab's products (or decorations) in a row, the main building's upgrade button below.
        public static BuildBarView Build(Transform parent, ProductButtonView buttonPrefab)
        {
            RectTransform bar = UiKit.Rect("BuildBar", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-40f, 410f));
            UiKit.AddImage(bar, UiKit.PanelColor);

            RectTransform tabs = Row(bar, "Tabs", -10f, 62f, 10f);
            string[] names = { "Stands", "Food", "Services", "Decor" };
            var tabButtons = new Object[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform tab = UiKit.Rect(names[i] + "Tab", tabs, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                tabButtons[i] = UiKit.AddButton(tab, Color.white, out _);
                UiKit.ChildText(tab, "Label", names[i], 34f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            }

            RectTransform row = Row(bar, "Products", -82f, 190f, 12f);
            RectTransform buildingRect = UiKit.Rect("BuildingButton", bar, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(-28f, 110f));
            Button buildingButton = UiKit.AddButton(buildingRect, UiKit.ButtonColor, out _);
            TextMeshProUGUI buildingLabel = UiKit.ChildText(buildingRect, "Label", "Upgrade", 40f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var view = bar.gameObject.AddComponent<BuildBarView>();
            PrimitiveKit.SetReference(view, "productButtonPrefab", buttonPrefab);
            PrimitiveKit.SetReference(view, "decorTilePrefab", CommunityUiBuilder.BuildDecorTilePrefab());
            PrimitiveKit.SetReference(view, "productRow", row);
            PrimitiveKit.SetReferences(view, "tabButtons", tabButtons);
            HudBuilder.SetColor(view, "tabColor", new Color(0.72f, 0.74f, 0.78f));
            HudBuilder.SetColor(view, "activeTabColor", UiKit.ButtonColor);
            PrimitiveKit.SetReference(view, "buildingButton", buildingButton);
            PrimitiveKit.SetReference(view, "buildingLabel", buildingLabel);
            return view;
        }

        private static RectTransform Row(RectTransform bar, string name, float y, float height, float spacing)
        {
            RectTransform row = UiKit.Rect(name, bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(-28f, height));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }
    }
}
