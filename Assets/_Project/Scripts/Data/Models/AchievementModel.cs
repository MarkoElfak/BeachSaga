using System;

namespace BeachTycoon.Data
{
    /// <summary>Saved progress of one achievement, matched to its definition by id.</summary>
    [Serializable]
    public sealed class AchievementModel
    {
        public string id = string.Empty;
        public long progress;
        public bool claimed;
    }
}
