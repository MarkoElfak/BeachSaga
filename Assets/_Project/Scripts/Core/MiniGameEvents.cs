namespace BeachTycoon.Core
{
    /// <summary>The player asked to play the mini-game of a stand (its Play button).</summary>
    public readonly struct MiniGameRequested
    {
        public readonly int StandIndex;

        public MiniGameRequested(int standIndex)
        {
            StandIndex = standIndex;
        }
    }

    /// <summary>A mini-game level was passed for the first time; LevelsPassed is the new count.</summary>
    public readonly struct MiniGameLevelPassed
    {
        public readonly string GameId;
        public readonly string ProductId;
        public readonly int LevelsPassed;

        public MiniGameLevelPassed(string gameId, string productId, int levelsPassed)
        {
            GameId = gameId;
            ProductId = productId;
            LevelsPassed = levelsPassed;
        }
    }
}
