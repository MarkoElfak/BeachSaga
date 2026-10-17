using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Mini-game progress and rewards: which game a product's stand offers, how many of its levels are passed
    /// (saved, kept across seasons) and the permanent sales bonus that gives the product.
    /// </summary>
    public sealed class MiniGameService
    {
        private readonly MiniGameDefinition[] _games;
        private readonly List<MiniGameProgress> _progress;
        private readonly IAnalyticsService _analytics;

        public MiniGameService(GameDatabase database, List<MiniGameProgress> progress, IAnalyticsService analytics)
        {
            _games = database.miniGames ?? new MiniGameDefinition[0];
            _progress = progress;
            _analytics = analytics;
        }

        public int Count => _games.Length;
        public MiniGameDefinition Get(int index) => _games[index];

        public MiniGameDefinition ForProduct(string productId)
        {
            for (int i = 0; i < _games.Length; i++)
            {
                MiniGameDefinition game = _games[i];
                if (game != null && game.product != null && game.product.id == productId) return game;
            }

            return null;
        }

        public int LevelsPassed(MiniGameDefinition game)
        {
            MiniGameProgress progress = Find(game.id);
            return progress != null ? progress.levelsPassed : 0;
        }

        public int LevelsPassed(string productId)
        {
            MiniGameDefinition game = ForProduct(productId);
            return game != null ? LevelsPassed(game) : 0;
        }

        public bool IsComplete(MiniGameDefinition game) => LevelsPassed(game) >= game.LevelCount;

        /// <summary>The level to play next (0-based); the last one again once every level is passed.</summary>
        public int NextLevelIndex(MiniGameDefinition game) =>
            System.Math.Max(0, System.Math.Min(LevelsPassed(game), game.LevelCount - 1));

        /// <summary>1 + levels passed x the game's bonus per level, for every sale of this product.</summary>
        public float IncomeMultiplier(ProductDefinition product)
        {
            if (product == null) return 1f;
            MiniGameDefinition game = ForProduct(product.id);
            return game != null ? IncomeMultiplier(game, LevelsPassed(game)) : 1f;
        }

        public static float IncomeMultiplier(MiniGameDefinition game, int levelsPassed) =>
            1f + System.Math.Max(0, levelsPassed) * game.incomeBonusPerLevel;

        /// <summary>
        /// Records a finished run. Passing the next unpassed level counts it (bonus, stand decoration); replays
        /// and lost runs change nothing. Returns true when a new level was passed.
        /// </summary>
        public bool ReportResult(MiniGameDefinition game, int levelIndex, bool won)
        {
            int passed = LevelsPassed(game);
            if (!won || levelIndex != passed || passed >= game.LevelCount) return false;

            MiniGameProgress progress = Find(game.id);
            if (progress == null)
            {
                progress = new MiniGameProgress { gameId = game.id };
                _progress.Add(progress);
            }

            progress.levelsPassed = passed + 1;
            string productId = game.product != null ? game.product.id : string.Empty;
            EventBus.Publish(new MiniGameLevelPassed(game.id, productId, progress.levelsPassed));
            _analytics.LogEvent(AnalyticsEvents.MiniGameLevelPassed, game.id, progress.levelsPassed);
            return true;
        }

        private MiniGameProgress Find(string gameId)
        {
            for (int i = 0; i < _progress.Count; i++)
            {
                if (_progress[i].gameId == gameId) return _progress[i];
            }

            return null;
        }
    }
}
