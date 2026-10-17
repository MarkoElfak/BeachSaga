using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Screens of the moving games: surf dodge and sky builder (positions measured from the bottom centre).</summary>
    internal static partial class MiniGameUiBuilder
    {
        private const int SurfObstacles = 24;
        private const int TowerFloors = 8;
        private static readonly Vector2 BottomCentre = new Vector2(0.5f, 0f);

        private static MiniGameView SurfDodge(RectTransform area)
        {
            RectTransform screen = UiKit.Stretch("SurfDodge", area);
            RectTransform sea = UiKit.Rect("Sea", screen, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 1120f));
            UiKit.AddImage(sea, new Color(0.2f, 0.6f, 0.85f), false);
            sea.gameObject.AddComponent<RectMask2D>();

            var waves = new RectTransform[6]; // short crests spread over the sea; the view scrolls them down
            for (int i = 0; i < waves.Length; i++)
            {
                var at = new Vector2((i * 2 % 5 - 2) * 170f + (i == 5 ? 85f : 0f), 0f);
                waves[i] = UiKit.Rect($"Wave{i}", sea, BottomCentre, BottomCentre, new Vector2(0.5f, 0.5f), at, new Vector2(150f, 10f));
                UiKit.AddImage(waves[i], new Color(1f, 1f, 1f, 0.35f), false);
            }

            var lanes = new RectTransform[SurfDodgeGame.MaxLanes - 1];
            for (int i = 0; i < lanes.Length; i++)
            {
                lanes[i] = UiKit.Rect($"Lane{i}", sea, BottomCentre, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 0f));
                UiKit.AddImage(lanes[i], new Color(1f, 1f, 1f, 0.18f), false);
            }

            var obstacles = new Image[SurfObstacles];
            for (int i = 0; i < obstacles.Length; i++)
            {
                obstacles[i] = AtBottom(sea, $"Obstacle{i}", new Vector2(130f, 110f), Color.gray, true);
                obstacles[i].gameObject.SetActive(false);
            }

            Image board = AtBottom(sea, "Board", new Vector2(80f, 180f), new Color(1f, 0.85f, 0.3f), true);
            Shape(board.transform, "Stripe", Vector2.zero, new Vector2(16f, 150f), new Color(0.95f, 0.35f, 0.3f));
            Shape(board.transform, "Surfer", new Vector2(0f, 10f), new Vector2(60f, 60f), Skin, true);

            Button left = SteerButton(screen, "LeftButton", "<", -235f);
            Button right = SteerButton(screen, "RightButton", ">", 235f);

            var view = screen.gameObject.AddComponent<SurfDodgeView>();
            PrimitiveKit.SetReference(view, "sea", sea);
            PrimitiveKit.SetReference(view, "board", board.rectTransform);
            PrimitiveKit.SetReference(view, "boardImage", board);
            PrimitiveKit.SetReference(view, "leftButton", left);
            PrimitiveKit.SetReference(view, "rightButton", right);
            PrimitiveKit.SetReferences(view, "laneLines", lanes);
            PrimitiveKit.SetReferences(view, "waves", waves);
            PrimitiveKit.SetReferences(view, "obstacles", obstacles);
            return view;
        }

        private static MiniGameView SkyBuilder(RectTransform area)
        {
            RectTransform screen = UiKit.Stretch("SkyBuilder", area);
            UiKit.AddImage(screen, new Color(0.7f, 0.88f, 1f)); // sky; also catches the taps
            Button drop = screen.gameObject.AddComponent<Button>();
            drop.transition = Selectable.Transition.None;
            screen.gameObject.AddComponent<RectMask2D>();

            AtBottom(screen, "Ground", new Vector2(920f, 60f), new Color(0.95f, 0.85f, 0.6f), false).rectTransform.pivot = new Vector2(0.5f, 0f);
            var floors = new RectTransform[TowerFloors];
            for (int i = 0; i < floors.Length; i++)
            {
                floors[i] = Floor(screen, $"Floor{i}", i % 2 == 0 ? new Color(0.95f, 0.9f, 0.8f) : new Color(0.9f, 0.8f, 0.7f));
                floors[i].gameObject.SetActive(false);
            }

            RectTransform slider = Floor(screen, "Slider", new Color(0.98f, 0.75f, 0.45f));
            RectTransform falling = Floor(screen, "Falling", new Color(0.9f, 0.45f, 0.4f));
            falling.gameObject.SetActive(false);
            var guides = new RectTransform[2];
            for (int i = 0; i < guides.Length; i++)
            {
                guides[i] = AtBottom(screen, $"Guide{i}", new Vector2(8f, 170f), new Color(0.2f, 0.45f, 0.75f, 0.6f), false).rectTransform;
            }

            RectTransform hintRect = UiKit.Rect("Hint", screen, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(860f, 70f));
            UiKit.AddText(hintRect, "Tap to drop", 44f, TextAlignmentOptions.Center, UiKit.TextDark);

            var view = screen.gameObject.AddComponent<SkyBuilderView>();
            PrimitiveKit.SetReference(view, "area", screen);
            PrimitiveKit.SetReference(view, "dropButton", drop);
            PrimitiveKit.SetReferences(view, "floors", floors);
            PrimitiveKit.SetReference(view, "slider", slider);
            PrimitiveKit.SetReference(view, "falling", falling);
            PrimitiveKit.SetReferences(view, "guides", guides);
            return view;
        }

        // A floor of the tower: a wall with three windows, 300 x 110.
        private static RectTransform Floor(RectTransform parent, string name, Color wall)
        {
            RectTransform floor = AtBottom(parent, name, new Vector2(300f, 110f), wall, false).rectTransform;
            for (int w = -1; w <= 1; w++) Shape(floor, $"Window{w + 1}", new Vector2(w * 90f, 5f), new Vector2(54f, 54f), new Color(0.45f, 0.7f, 0.95f));
            return floor;
        }

        private static Image AtBottom(RectTransform parent, string name, Vector2 size, Color color, bool round)
        {
            RectTransform rect = UiKit.Rect(name, parent, BottomCentre, BottomCentre, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Image image = UiKit.AddImage(rect, color, false);
            if (round) image.sprite = Circle;
            return image;
        }

        private static Button SteerButton(RectTransform screen, string name, string label, float x)
        {
            RectTransform rect = UiKit.Rect(name, screen, BottomCentre, BottomCentre, BottomCentre, new Vector2(x, 20f), new Vector2(430f, 210f));
            Button button = UiKit.AddButton(rect, Soft, out _);
            UiKit.ChildText(rect, "Label", label, 110f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }
    }
}
