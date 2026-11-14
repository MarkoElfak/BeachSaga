using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace BeachTycoon.Tests
{
    /// <summary>
    /// Every level of every real mini-game (run Setup first) can be passed by good play (the built-in autoplayer,
    /// human-paced) and is lost by careless play, for several random seeds.
    /// </summary>
    public sealed class MiniGameTests
    {
        private const float Step = 1f / 60f;
        private const float MaxSeconds = 600f;
        private static readonly int[] Seeds = { 1, 7, 42, 1234 };

        private GameDatabase _database;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            _database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
            if (_database.miniGames == null || _database.miniGames.Length == 0) Assert.Ignore("No mini-games yet; run Setup.");
        }

        [Test]
        public void EveryStandHasAMiniGameWithFiveLevels()
        {
            foreach (ProductDefinition product in _database.products)
            {
                if (product.IsService) continue; // a chapel or first aid sells nothing, so it has no game
                MiniGameDefinition game = System.Array.Find(_database.miniGames, g => g != null && g.product == product);
                Assert.IsNotNull(game, product.id);
                Assert.AreEqual(5, game.LevelCount, game.id);
                for (int i = 1; i < game.LevelCount; i++)
                {
                    Assert.GreaterOrEqual(game.levels[i].target, game.levels[i - 1].target, $"{game.id} level {i + 1} asks for less");
                }
            }
        }

        [Test]
        public void EveryLevelCanBePassed()
        {
            ForEachLevel((game, level, seed) =>
            {
                MiniGame run = Play(game.kind, level, seed, g => g.AutoPlay());
                Assert.AreEqual(MiniGameState.Won, run.State, $"{game.id} seed {seed}: {run.Score}/{run.Target}, {run.Mistakes} misses");
            });
        }

        [Test]
        public void CarelessPlayLosesEveryLevel()
        {
            ForEachLevel((game, level, seed) =>
            {
                MiniGame run = Play(game.kind, level, seed, Careless);
                Assert.AreEqual(MiniGameState.Lost, run.State, $"{game.id} seed {seed}: {run.Score}/{run.Target}");
            });
        }

        private void ForEachLevel(System.Action<MiniGameDefinition, MiniGameLevel, int> check)
        {
            foreach (MiniGameDefinition game in _database.miniGames)
            {
                foreach (MiniGameLevel level in game.levels)
                {
                    foreach (int seed in Seeds) check(game, level, seed);
                }
            }
        }

        private static MiniGame Play(MiniGameKind kind, MiniGameLevel level, int seed, System.Action<MiniGame> input)
        {
            MiniGame game = MiniGame.Create(kind);
            game.Begin(level, seed);
            for (float t = 0f; t < MaxSeconds && !game.IsOver; t += Step)
            {
                input(game);
                game.Tick(Step);
            }

            return game;
        }

        // Ignoring the customers and visitors, never letting go of the tap, dropping at once, steering into rocks.
        private static void Careless(MiniGame game)
        {
            switch (game)
            {
                case PerfectPourGame pour:
                    pour.StartPour();
                    break;
                case SkyBuilderGame sky:
                    if (System.Math.Abs(sky.FloorX) > sky.Level.tolerance) sky.Drop();
                    break;
                case SurfDodgeGame surf:
                    SteerIntoRocks(surf);
                    break;
            }
        }

        private static void SteerIntoRocks(SurfDodgeGame surf)
        {
            int next = -1;
            for (int row = 0; row < SurfDodgeGame.MaxRows; row++)
            {
                bool coming = surf.IsRowActive(row) && surf.RowY(row) > SurfDodgeGame.BoardY + SurfDodgeGame.HitRange;
                if (coming && (next < 0 || surf.RowY(row) < surf.RowY(next))) next = row;
            }

            if (next < 0 || surf.Blocks(next, surf.BoardLane)) return;
            for (int lane = 0; lane < surf.Lanes; lane++)
            {
                if (surf.Blocks(next, lane)) surf.Steer(lane - surf.BoardLane);
            }
        }
    }
}
