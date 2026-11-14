using System;
using System.Collections.Generic;

namespace BeachTycoon.Data
{
    /// <summary>
    /// Everything that persists between sessions. Services work directly on these model objects,
    /// so saving is a single JSON serialization of this class.
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        /// <summary>Bump when the layout changes and add an ISaveMigration from the previous version.</summary>
        public const int CurrentVersion = 18;

        public int version = CurrentVersion;

        /// <summary>UTC time of the last save, ISO 8601 round-trip format. Used for offline earnings.</summary>
        public string lastSeenUtc = string.Empty;

        public WalletModel wallet = new WalletModel();
        public List<LotModel> lots = new List<LotModel>();
        public List<StandModel> stands = new List<StandModel>();

        // Version 2 (Milestone 2)
        public List<NeighbourModel> neighbours = new List<NeighbourModel>();
        public List<DecorationModel> decorations = new List<DecorationModel>();

        // Version 3 (Milestone 3)
        public MetaModel meta = new MetaModel();

        // Version 8 (achievements)
        public List<AchievementModel> achievements = new List<AchievementModel>();

        // Version 12: mini-game levels passed, per game.
        public List<MiniGameProgress> miniGames = new List<MiniGameProgress>();

        // Version 18: hired workers and the job seekers waiting on the beach.
        public List<WorkerModel> staff = new List<WorkerModel>();
        public List<WorkerModel> jobSeekers = new List<WorkerModel>();

        // Version 9: lots, stands and decorations have a yaw (see LotModel.yaw).
        // Version 10: stands can have a manager (StandModel.manager).
        // Version 11: seasons (MetaModel.seashells, seasonsCompleted; WalletModel.seasonEarned).
        // Version 12: mini-games (miniGames).
        // Version 13: sound and music volumes, vibration (SettingsModel).
        // Version 14: privacy choice (SettingsModel.privacy).
        // Version 15: village name and culture (MetaModel.villageName, cultureId).
        // Version 16: bigger map; house lots 5-12 moved, so old houses there were pinned (LotModel.placed).
        // Version 17: the player's character (MetaModel.characterStand).
        // Version 18: staff and job seekers; each hired manager became a level 3 worker at their stand.

        public static GameSaveData CreateNew(long startingCoins)
        {
            return new GameSaveData { wallet = new WalletModel(startingCoins) };
        }
    }

    /// <summary>Reads only the version field, so old files can be migrated before full parsing.</summary>
    [Serializable]
    public sealed class SaveVersionHeader
    {
        public int version;
    }
}
