using BeachTycoon.Data;
using UnityEditor;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// One mini-game per stand, five levels each from easy to hard. Values are written only on creation, so
    /// difficulty tuned in the Inspector (ScriptableObjects/MiniGames) survives Setup.
    /// </summary>
    internal static class MiniGameAssetBuilder
    {
        private const string Folder = ProjectPaths.Data + "/MiniGames";

        public static void Fill(GameDatabase database)
        {
            ProjectPaths.EnsureFolder(Folder);
            database.miniGames = new[]
            {
                Make(database, "DonutRush", MiniGameKind.DonutRush, "donuts", "Donut rush",
                    "A customer shows the icing they want. Tap that icing before their patience runs out.",
                    L(6, 3, patience: 5f, choices: 3), L(8, 3, patience: 4f, choices: 3), L(10, 3, patience: 3.5f, choices: 4),
                    L(12, 2, patience: 3f, choices: 5), L(14, 2, patience: 2.6f, choices: 6)),
                Make(database, "TowelRun", MiniGameKind.TowelRun, "towels", "Towel run",
                    "Visitors lie down on the sunbeds. Tap each one to hand them a towel before they give up.",
                    L(6, 3, pace: 1.6f, patience: 3.5f, choices: 4), L(8, 3, pace: 1.4f, patience: 3f, choices: 6),
                    L(10, 3, pace: 1.2f, patience: 2.7f, choices: 6), L(12, 2, pace: 1f, patience: 2.4f, choices: 9),
                    L(15, 2, pace: 0.85f, patience: 2.1f, choices: 9)),
                Make(database, "PerfectPour", MiniGameKind.PerfectPour, "drinks", "Perfect pour",
                    "Hold to fill the glass and let go while the drink is inside the green band. Don't let it overflow!",
                    L(4, 3, speed: 0.35f, tolerance: 0.3f), L(5, 3, speed: 0.45f, tolerance: 0.25f), L(6, 3, speed: 0.55f, tolerance: 0.2f),
                    L(7, 2, speed: 0.65f, tolerance: 0.16f), L(8, 2, speed: 0.75f, tolerance: 0.13f)),
                Make(database, "SurfDodge", MiniGameKind.SurfDodge, "surf_lessons", "Surf dodge",
                    "Steer the board left and right to dodge the rocks and buoys. Get past every row to pass.",
                    L(8, 3, speed: 0.35f, pace: 1.6f, choices: 3), L(12, 3, speed: 0.42f, pace: 1.4f, choices: 3),
                    L(16, 3, speed: 0.5f, pace: 1.2f, choices: 4), L(20, 2, speed: 0.58f, pace: 1.05f, choices: 4),
                    L(25, 2, speed: 0.65f, pace: 0.95f, choices: 5)),
                Make(database, "SkyBuilder", MiniGameKind.SkyBuilder, "houses", "Sky builder",
                    "A new floor slides from side to side. Tap to drop it between the marks, right on top of the tower.",
                    L(5, 3, speed: 0.5f, tolerance: 0.3f), L(6, 3, speed: 0.65f, tolerance: 0.25f), L(8, 3, speed: 0.8f, tolerance: 0.2f),
                    L(10, 2, speed: 1f, tolerance: 0.16f), L(12, 2, speed: 1.2f, tolerance: 0.12f)),
            };
        }

        internal static MiniGameDefinition Make(GameDatabase database, string id, MiniGameKind kind, string productId,
            string title, string howToPlay, params MiniGameLevel[] levels)
        {
            var game = DataAssetBuilder.LoadOrCreate<MiniGameDefinition>($"{Folder}/{id}.asset", out bool created);
            if (created)
            {
                game.id = id;
                game.kind = kind;
                game.title = title;
                game.howToPlay = howToPlay;
                game.incomeBonusPerLevel = 0.1f;
                game.levels = levels;
            }

            game.product = database.FindProduct(productId);
            EditorUtility.SetDirty(game);
            return game;
        }

        internal static MiniGameLevel L(int target, int lives, float speed = 0f, float pace = 1f, float patience = 3f,
            float tolerance = 0.2f, int choices = 3, float timeLimit = 0f)
        {
            return new MiniGameLevel
            {
                target = target, lives = lives, speed = speed, pace = pace, patience = patience,
                tolerance = tolerance, choices = choices, timeLimit = timeLimit,
            };
        }
    }
}
