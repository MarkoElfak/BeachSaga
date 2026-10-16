using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>Owns the live GameSaveData and persists it as versioned JSON.</summary>
    public interface ISaveService
    {
        GameSaveData Data { get; }

        /// <summary>True when Data came from an existing save file rather than a new game.</summary>
        bool LoadedFromDisk { get; }

        string FilePath { get; }

        /// <summary>When the loaded save was last written (start of the absence), or null for a new game.</summary>
        System.DateTime? PreviousSessionUtc { get; }

        void Save();
    }

    /// <summary>Upgrades raw save JSON from FromVersion to FromVersion + 1.</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        string Migrate(string json);
    }
}
