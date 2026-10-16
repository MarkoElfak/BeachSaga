using BeachTycoon.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Builds the Beach HUD: top bar (coins, popularity), bottom build bar, floating text and upgrade panel.</summary>
    internal static class HudBuilder
    {
        private const string UiPrefabs = ProjectPaths.Prefabs + "/UI";
        private const string ProductButtonPath = UiPrefabs + "/ProductButton.prefab";
        private const string FloatingTextPath = UiPrefabs + "/FloatingText.prefab";
        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        public static void Build(Camera worldCamera, BeachTycoon.Gameplay.CoinSpawner coins)
        {
            ProjectPaths.EnsureFolder(UiPrefabs);
            var buttonPrefab = BuildProductButtonPrefab();
            var floatingPrefab = BuildFloatingTextPrefab();

            RectTransform safe = CreateCanvas("HUD");
            BuildTopBar(safe);
            BuildBarView buildBar = BuildBuildBar(safe, buttonPrefab);

            RectTransform floatingLayer = UiKit.Stretch("FloatingText", safe);
            var floating = floatingLayer.gameObject.AddComponent<FloatingTextSpawner>();
            PrimitiveKit.SetReference(floating, "itemPrefab", floatingPrefab);
            PrimitiveKit.SetReference(floating, "layer", floatingLayer);
            PrimitiveKit.SetReference(floating, "worldCamera", worldCamera);

            CommunityUiBuilder.BuildQuestTracker(safe);
            EventUiBuilder.BuildEventBanner(safe);
            CommunityUiBuilder.BuildNeighbourRequest(safe);
            CommunityUiBuilder.BuildToast(safe);
            MetaUiBuilder.BuildBoostButton(safe);
            MetaUiBuilder.BuildTutorial(safe, worldCamera, coins, buildBar);
            PlacementSceneBuilder.BuildMoveHint(safe);
            PlacementSceneBuilder.BuildSelectionArrows(safe, worldCamera);
            UpgradePanelBuilder.Build(safe);
            DecorPanelView decorPanel = CommunityUiBuilder.BuildDecorPanel(safe);
            PrimitiveKit.SetReference(buildBar, "decorPanel", decorPanel);
            SettingsUiBuilder.Build(safe);
            MetaUiBuilder.BuildDailyRewards(safe);
            OnlineUiBuilder.BuildVisitPanel(safe);
            AchievementUiBuilder.Build(safe);
            SeasonUiBuilder.Build(safe);
            MiniGameUiBuilder.Build(safe);
            MetaUiBuilder.BuildOfflinePopup(safe);
            BringWindowsToFront(safe);
            FeelUiBuilder.BuildConfetti(safe); // over everything, windows included
        }

        // A HUD button built after a window would draw (and take taps) over its backdrop, so windows go last.
        private static void BringWindowsToFront(RectTransform safe)
        {
            foreach (ModalWindow window in safe.GetComponentsInChildren<ModalWindow>(true))
            {
                Transform host = window.transform;
                while (host.parent != safe) host = host.parent;
                host.SetAsLastSibling();
            }
        }

        /// <summary>Portrait overlay canvas scaled from 1080x1920; returns its safe-area root.</summary>
        public static RectTransform CreateCanvas(string name)
        {
            var canvasGo = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform safe = UiKit.Stretch("SafeArea", canvasGo.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            return safe;
        }

        private static void BuildTopBar(Transform parent)
        {
            BuildCoinCounter(parent);
            BuildPopularity(parent);
        }

        public static void BuildCoinCounter(Transform parent)
        {
            RectTransform coins = UiKit.Rect("CoinPanel", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -24f), new Vector2(420f, 110f));
            UiKit.AddImage(coins, UiKit.PanelColor);
            RectTransform coinIcon = UiKit.Rect("Icon", coins, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(62f, 0f), new Vector2(64f, 64f));
            UiKit.AddImage(coinIcon, new Color(1f, 0.79f, 0.24f), false);
            RectTransform coinLabelRect = UiKit.Rect("Label", coins, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(-130f, 0f));
            TextMeshProUGUI coinLabel = UiKit.AddText(coinLabelRect, "0", 60f, TextAlignmentOptions.MidlineLeft, Color.white, FontStyles.Bold);
            PrimitiveKit.SetReference(coins.gameObject.AddComponent<CoinCounterView>(), "label", coinLabel);
        }

        private static void BuildPopularity(Transform parent)
        {
            RectTransform pop = UiKit.Rect("PopularityPanel", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -24f), new Vector2(460f, 110f));
            UiKit.AddImage(pop, UiKit.PanelColor);
            RectTransform popIcon = UiKit.Rect("Icon", pop, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, 0f), new Vector2(52f, 52f));
            UiKit.AddImage(popIcon, new Color(0.95f, 0.45f, 0.65f), false);
            RectTransform popLabelRect = UiKit.Rect("Label", pop, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(-115f, 0f));
            TextMeshProUGUI popLabel = UiKit.AddText(popLabelRect, "Popularity 0.00", 40f, TextAlignmentOptions.MidlineLeft, Color.white);
            PrimitiveKit.SetReference(pop.gameObject.AddComponent<PopularityLabelView>(), "label", popLabel);
        }

        private static BuildBarView BuildBuildBar(Transform parent, ProductButtonView buttonPrefab)
        {
            RectTransform bar = UiKit.Rect("BuildBar", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-40f, 340f));
            UiKit.AddImage(bar, UiKit.PanelColor);

            RectTransform row = UiKit.Rect("Products", bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-28f, 190f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            RectTransform buildingRect = UiKit.Rect("BuildingButton", bar, new Vector2(0f, 0f), new Vector2(0.72f, 0f), new Vector2(0f, 0f), new Vector2(14f, 14f), new Vector2(-21f, 110f));
            Button buildingButton = UiKit.AddButton(buildingRect, UiKit.ButtonColor, out _);
            TextMeshProUGUI buildingLabel = UiKit.ChildText(buildingRect, "Label", "Upgrade", 40f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            RectTransform decorRect = UiKit.Rect("DecorButton", bar, new Vector2(0.72f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(7f, 14f), new Vector2(-21f, 110f));
            Button decorButton = UiKit.AddButton(decorRect, new Color(0.55f, 0.85f, 0.75f), out _);
            UiKit.ChildText(decorRect, "Label", "Decor", 40f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);

            var view = bar.gameObject.AddComponent<BuildBarView>();
            PrimitiveKit.SetReference(view, "productButtonPrefab", buttonPrefab);
            PrimitiveKit.SetReference(view, "productRow", row);
            PrimitiveKit.SetReference(view, "buildingButton", buildingButton);
            PrimitiveKit.SetReference(view, "buildingLabel", buildingLabel);
            PrimitiveKit.SetReference(view, "decorButton", decorButton);
            return view;
        }

        private static ProductButtonView BuildProductButtonPrefab()
        {
            var root = new GameObject("ProductButton", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(190f, 190f);
            Button button = UiKit.AddButton(rect, Color.white, out Image background);

            TextMeshProUGUI nameLabel = Line(rect, "Name", 0.66f, 1f, 34f, FontStyles.Bold);
            TextMeshProUGUI statusLabel = Line(rect, "Status", 0.38f, 0.66f, 30f, FontStyles.Normal);
            TextMeshProUGUI actionLabel = Line(rect, "Action", 0.04f, 0.38f, 30f, FontStyles.Bold);

            var view = root.AddComponent<ProductButtonView>();
            PrimitiveKit.SetReference(view, "button", button);
            PrimitiveKit.SetReference(view, "background", background);
            PrimitiveKit.SetReference(view, "nameLabel", nameLabel);
            PrimitiveKit.SetReference(view, "statusLabel", statusLabel);
            PrimitiveKit.SetReference(view, "actionLabel", actionLabel);
            SetColor(view, "lockedColor", new Color(0.6f, 0.6f, 0.6f));
            SetColor(view, "unlockColor", UiKit.ButtonColor);
            SetColor(view, "ownedColor", new Color(0.93f, 0.97f, 1f));

            return PrimitiveKit.SavePrefab(root, ProductButtonPath).GetComponent<ProductButtonView>();
        }

        private static TextMeshProUGUI Line(RectTransform parent, string name, float yMin, float yMax, float size, FontStyles style)
        {
            RectTransform rect = UiKit.Rect(name, parent, new Vector2(0f, yMin), new Vector2(1f, yMax), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-12f, 0f));
            return UiKit.AddText(rect, name, size, TextAlignmentOptions.Center, UiKit.TextDark, style);
        }

        private static FloatingTextItem BuildFloatingTextPrefab()
        {
            var root = new GameObject("FloatingText", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(320f, 90f);
            TextMeshProUGUI label = UiKit.AddText(rect, "+0", 64f, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.2f), FontStyles.Bold);
            label.outlineWidth = 0.2f;
            label.outlineColor = new Color32(70, 40, 0, 255);
            var item = root.AddComponent<FloatingTextItem>();
            PrimitiveKit.SetReference(item, "label", label);
            return PrimitiveKit.SavePrefab(root, FloatingTextPath).GetComponent<FloatingTextItem>();
        }

        private static void SetColor(Object target, string field, Color color)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
