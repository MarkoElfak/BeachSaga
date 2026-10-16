using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>Reads the island's current value for each level goal (tier, neighbours, decorations...).</summary>
    public sealed class AchievementLevels
    {
        private readonly BuildingManager _buildings;
        private readonly StandService _stands;
        private readonly NeighbourService _neighbours;
        private readonly GameSaveData _data;

        public AchievementLevels(BuildingManager buildings, StandService stands, NeighbourService neighbours, GameSaveData data)
        {
            _buildings = buildings;
            _stands = stands;
            _neighbours = neighbours;
            _data = data;
        }

        public long Read(AchievementGoal goal)
        {
            switch (goal)
            {
                case AchievementGoal.ReachTier: return _buildings.MainTier;
                case AchievementGoal.HostNeighbours: return _neighbours.Count;
                case AchievementGoal.OwnDecorations: return _data.decorations.Count;
                case AchievementGoal.StandLevel: return HighestStandLevel();
                case AchievementGoal.DailyStreak: return _data.meta.dailyStreak;
                default: return 0;
            }
        }

        private int HighestStandLevel()
        {
            int best = 0;
            for (int i = 0; i < _stands.Count; i++)
            {
                int level = _stands.Get(i).Model.level;
                if (level > best) best = level;
            }

            return best;
        }
    }
}
