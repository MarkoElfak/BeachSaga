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
        public const int CurrentVersion = 4;

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
