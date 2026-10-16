using BeachTycoon.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The mini-game window: intro, game and result pages, and one screen per game (built in the .Screens files).
    /// Game screens draw in a 920 x 1380 area with simple shapes (squares and the built-in round knob sprite).
    /// </summary>
    internal static partial class MiniGameUiBuilder
    {
        private static readonly Color Go = new Color(0.45f, 0.8f, 0.45f);
        private static readonly Color Soft = new Color(0.55f, 0.8f, 0.95f);
        private static Sprite _circle;

        private static Sprite Circle => _circle != null ? _circle : _circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        public static void Build(Transform parent)
        {
            RectTransform host = UiKit.Stretch("MiniGamePanel", parent);
            RectTransform root = MetaUiBuilder.Modal(host, "Mini-game", new Vector2(1000f, 1700f), out RectTransform window, out Button close);
            TMP_Text title = window.Find("Title").GetComponent<TMP_Text>();

            RectTransform intro = UiKit.Stretch("IntroPage", window);
            TMP_Text howTo = Text(intro, "HowToPlay", -170f, 280f, 42f, FontStyles.Normal);
            TMP_Text level = Text(intro, "LevelInfo", -500f, 100f, 56f, FontStyles.Bold);
            TMP_Text reward = Text(intro, "RewardInfo", -620f, 240f, 40f, FontStyles.Normal);
            Button play = WideButton(intro, "PlayButton", -1420f, Go, out TMP_Text playLabel);

            RectTransform game = UiKit.Stretch("GamePage", window);
            TMP_Text score = Hud(game, "Score", new Vector2(0f, 1f), TextAlignmentOptions.Left);
            TMP_Text lives = Hud(game, "Lives", new Vector2(1f, 1f), TextAlignmentOptions.Right);
            RectTransform timer = UiKit.Rect("TimerBar", game, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -235f), new Vector2(900f, 22f));
            UiKit.AddImage(timer, new Color(0f, 0f, 0f, 0.15f), false);
            RectTransform timerFill = Fill(timer, new Color(0.98f, 0.62f, 0.3f));
            RectTransform area = UiKit.Rect("Area", game, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(920f, 1380f));
            MiniGameView[] views = { DonutRush(area), TowelRun(area), PerfectPour(area), SurfDodge(area), SkyBuilder(area) };

            RectTransform result = UiKit.Stretch("ResultPage", window);
            TMP_Text resultTitle = Text(result, "ResultTitle", -420f, 130f, 68f, FontStyles.Bold);
            TMP_Text resultBody = Text(result, "ResultBody", -580f, 260f, 44f, FontStyles.Normal);
            Button again = WideButton(result, "AgainButton", -1150f, Go, out TMP_Text againLabel);
            Button back = WideButton(result, "BackButton", -1320f, Soft, out TMP_Text backLabel);
            backLabel.text = "Back";

            var view = host.gameObject.AddComponent<MiniGamePanelView>();
            PrimitiveKit.SetReference(view, "root", root.gameObject);
            PrimitiveKit.SetReference(view, "title", title);
            PrimitiveKit.SetReference(view, "closeButton", close);
            PrimitiveKit.SetReference(view, "introPage", intro.gameObject);
            PrimitiveKit.SetReference(view, "howToPlay", howTo);
            PrimitiveKit.SetReference(view, "levelInfo", level);
            PrimitiveKit.SetReference(view, "rewardInfo", reward);
            PrimitiveKit.SetReference(view, "playButton", play);
            PrimitiveKit.SetReference(view, "playLabel", playLabel);
            PrimitiveKit.SetReference(view, "gamePage", game.gameObject);
            PrimitiveKit.SetReference(view, "scoreText", score);
            PrimitiveKit.SetReference(view, "livesText", lives);
            PrimitiveKit.SetReference(view, "timerBar", timer.gameObject);
            PrimitiveKit.SetReference(view, "timerFill", timerFill);
            PrimitiveKit.SetReference(view, "resultPage", result.gameObject);
            PrimitiveKit.SetReference(view, "resultTitle", resultTitle);
            PrimitiveKit.SetReference(view, "resultBody", resultBody);
            PrimitiveKit.SetReference(view, "againButton", again);
            PrimitiveKit.SetReference(view, "againLabel", againLabel);
            PrimitiveKit.SetReference(view, "backButton", back);
            PrimitiveKit.SetReferences(view, "views", views);
            foreach (MiniGameView screen in views) screen.gameObject.SetActive(false);
            game.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }

        private static TMP_Text Text(RectTransform page, string name, float y, float height, float size, FontStyles style)
        {
            RectTransform rect = UiKit.Rect(name, page, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(-120f, height));
            TextMeshProUGUI label = UiKit.AddText(rect, name, size, TextAlignmentOptions.Center, UiKit.TextDark, style);
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.6f;
            label.fontSizeMax = size;
            return label;
        }

        private static TMP_Text Hud(RectTransform page, string name, Vector2 anchor, TextAlignmentOptions alignment)
        {
            RectTransform rect = UiKit.Rect(name, page, anchor, anchor, anchor, new Vector2(anchor.x > 0.5f ? -60f : 60f, -150f), new Vector2(420f, 70f));
            return UiKit.AddText(rect, name, 48f, alignment, UiKit.TextDark, FontStyles.Bold);
        }

        private static Button WideButton(RectTransform page, string name, float y, Color color, out TMP_Text label)
        {
            RectTransform rect = UiKit.Rect(name, page, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(660f, 140f));
            Button button = UiKit.AddButton(rect, color, out _);
            label = UiKit.ChildText(rect, "Label", name, 50f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }

        // Shapes for the game screens, positioned from the centre of their parent unless anchored otherwise.
        private static Image Shape(Transform parent, string name, Vector2 at, Vector2 size, Color color, bool round = false)
        {
            RectTransform rect = UiKit.Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), at, size);
            Image image = UiKit.AddImage(rect, color, false);
            if (round) image.sprite = Circle;
            return image;
        }

        /// <summary>A bar's fill: covers the bar, pivoted at its left edge so MiniGameView.SetFill can scale it.</summary>
        private static RectTransform Fill(RectTransform bar, Color color)
        {
            RectTransform fill = UiKit.Rect("Fill", bar, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            UiKit.AddImage(fill, color, false);
            return fill;
        }

        // A bar with a background and a fill, centred at 'at'.
        private static RectTransform Bar(Transform parent, string name, Vector2 at, Vector2 size, Color color)
        {
            Image back = Shape(parent, name, at, size, new Color(0f, 0f, 0f, 0.18f));
            return Fill(back.rectTransform, color);
        }
    }
}
