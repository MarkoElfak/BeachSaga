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
        public const int CurrentVersion = 15;

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

        // Version 9: lots, stands and decorations have a yaw (see LotModel.yaw).
        // Version 10: stands can have a manager (StandModel.manager).
        // Version 11: seasons (MetaModel.seashells, seasonsCompleted; WalletModel.seasonEarned).
        // Version 12: mini-games (miniGames).
        // Version 13: sound and music volumes, vibration (SettingsModel).
        // Version 14: privacy choice (SettingsModel.privacy).
        // Version 15: village name and culture (MetaModel.villageName, cultureId).

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
