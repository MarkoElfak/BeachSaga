using System;

namespace BeachTycoon.Data
{
    /// <summary>Levels passed in one mini-game (kept across seasons, like trophies).</summary>
    [Serializable]
    public sealed class MiniGameProgress
    {
        public string gameId;
        public int levelsPassed;
    }
}
