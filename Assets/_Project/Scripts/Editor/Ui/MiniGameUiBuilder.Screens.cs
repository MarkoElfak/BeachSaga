using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Screens of the tapping games: donut rush, towel run and perfect pour.</summary>
    internal static partial class MiniGameUiBuilder
    {
        private static readonly Color Dough = new Color(0.86f, 0.62f, 0.35f);
        private static readonly Color Skin = new Color(0.96f, 0.78f, 0.62f);
        private static readonly Color Bubble = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color Patience = new Color(0.45f, 0.8f, 0.45f);

        private static MiniGameView DonutRush(RectTransform area)
        {
            RectTransform screen = UiKit.Stretch("DonutRush", area);
            Shape(screen, "Floor", new Vector2(0f, -420f), new Vector2(920f, 540f), new Color(0.95f, 0.88f, 0.75f));
            Person(screen, new Vector2(-250f, 260f), new Color(0.4f, 0.6f, 0.9f));

            Image bubble = Shape(screen, "Bubble", new Vector2(170f, 420f), new Vector2(460f, 420f), Bubble, true);
            Shape(bubble.transform, "Dough", new Vector2(0f, 40f), new Vector2(240f, 240f), Dough, true);
            Image wanted = Shape(bubble.transform, "Icing", new Vector2(0f, 40f), new Vector2(190f, 190f), Color.white, true);
            Shape(bubble.transform, "Hole", new Vector2(0f, 40f), new Vector2(64f, 64f), Bubble, true);
            RectTransform labelRect = UiKit.Rect("Wanted", bubble.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(400f, 70f));
            TMP_Text wantedLabel = UiKit.AddText(labelRect, "Icing", 44f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            RectTransform patience = Bar(screen, "Patience", new Vector2(170f, 170f), new Vector2(400f, 32f), Patience);

            var buttons = new Button[DonutRushGame.MaxChoices];
            for (int i = 0; i < buttons.Length; i++)
            {
                var at = new Vector2(-290f + 290f * (i % 3), i < 3 ? -260f : -520f);
                RectTransform rect = UiKit.Rect($"Icing{i}", screen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), at, new Vector2(250f, 240f));
                buttons[i] = UiKit.AddButton(rect, new Color(1f, 1f, 1f, 0.001f), out _);
                Shape(rect, "Dough", new Vector2(0f, 25f), new Vector2(180f, 180f), Dough, true);
                Shape(rect, "Icing", new Vector2(0f, 25f), new Vector2(145f, 145f), DonutRushView.IcingColors[i], true);
                Shape(rect, "Hole", new Vector2(0f, 25f), new Vector2(46f, 46f), new Color(0.95f, 0.88f, 0.75f), true);
                RectTransform name = UiKit.Rect("Name", rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(250f, 50f));
                UiKit.AddText(name, DonutRushView.IcingNames[i], 34f, TextAlignmentOptions.Center, UiKit.TextDark);
            }

            var view = screen.gameObject.AddComponent<DonutRushView>();
            PrimitiveKit.SetReference(view, "wantedIcing", wanted);
            PrimitiveKit.SetReference(view, "wantedLabel", wantedLabel);
            PrimitiveKit.SetReference(view, "patienceFill", patience);
            PrimitiveKit.SetReferences(view, "icingButtons", buttons);
            return view;
        }

        private static MiniGameView TowelRun(RectTransform area)
        {
            RectTransform screen = UiKit.Stretch("TowelRun", area);
            UiKit.AddImage(screen, new Color(0.98f, 0.9f, 0.7f), false); // sand
            var beds = new Button[TowelRunGame.MaxBeds];
            var visitors = new GameObject[beds.Length];
            var fills = new RectTransform[beds.Length];
            Color[] towelColors = { new Color(0.9f, 0.35f, 0.35f), new Color(1f, 0.85f, 0.3f), new Color(0.3f, 0.75f, 0.75f) };
            for (int i = 0; i < beds.Length; i++)
            {
                RectTransform rect = UiKit.Rect($"Bed{i}", screen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 330f));
                beds[i] = UiKit.AddButton(rect, new Color(0.55f, 0.75f, 0.95f), out _);
                Shape(rect, "Pillow", new Vector2(0f, 120f), new Vector2(170f, 50f), Color.white);
                GameObject visitor = UiKit.Stretch("Visitor", rect).gameObject;
                Person(visitor.transform, new Vector2(0f, 20f), towelColors[i % towelColors.Length]);
                fills[i] = Bar(visitor.transform, "Wait", new Vector2(0f, -135f), new Vector2(190f, 24f), Patience);
                visitor.SetActive(false);
                visitors[i] = visitor;
            }

            var view = screen.gameObject.AddComponent<TowelRunView>();
            PrimitiveKit.SetReference(view, "area", screen);
            PrimitiveKit.SetReferences(view, "beds", beds);
            PrimitiveKit.SetReferences(view, "visitors", visitors);
            PrimitiveKit.SetReferences(view, "waitFills", fills);
            return view;
        }

        private static MiniGameView PerfectPour(RectTransform area)
        {
            RectTransform screen = UiKit.Stretch("PerfectPour", area);
            Shape(screen, "Bar", new Vector2(0f, -170f), new Vector2(920f, 60f), new Color(0.55f, 0.38f, 0.25f));
            Image glass = Shape(screen, "Glass", new Vector2(0f, 180f), new Vector2(360f, 640f), new Color(0.85f, 0.95f, 1f, 0.55f));
            RectTransform band = UiKit.Rect("Band", glass.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 0f));
            UiKit.AddImage(band, new Color(0.35f, 0.85f, 0.4f, 0.55f), false);
            RectTransform fill = UiKit.Rect("Drink", glass.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(-24f, 0f));
            UiKit.AddImage(fill, new Color(1f, 0.55f, 0.2f, 0.9f), false);
            Shape(glass.transform, "Rim", new Vector2(0f, 320f), new Vector2(380f, 16f), new Color(1f, 1f, 1f, 0.9f));

            RectTransform resultRect = UiKit.Rect("Result", screen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 570f), new Vector2(700f, 110f));
            TMP_Text result = UiKit.AddText(resultRect, "Perfect!", 72f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            resultRect.gameObject.SetActive(false);

            RectTransform holdRect = UiKit.Rect("PourButton", screen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -470f), new Vector2(660f, 220f));
            UiKit.AddImage(holdRect, Go);
            UiKit.ChildText(holdRect, "Label", "Hold to pour", 56f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            var hold = holdRect.gameObject.AddComponent<HoldButton>();

            var view = screen.gameObject.AddComponent<PerfectPourView>();
            PrimitiveKit.SetReference(view, "fill", fill);
            PrimitiveKit.SetReference(view, "band", band);
            PrimitiveKit.SetReference(view, "pourButton", hold);
            PrimitiveKit.SetReference(view, "result", result);
            return view;
        }

        // A simple beach-goer: round head over a shirt.
        private static void Person(Transform parent, Vector2 at, Color shirt)
        {
            Shape(parent, "Body", at + new Vector2(0f, -70f), new Vector2(130f, 130f), shirt);
            Shape(parent, "Head", at + new Vector2(0f, 45f), new Vector2(100f, 100f), Skin, true);
        }
    }
}
