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

        /// <summary>How the save was loaded when the saved file could not be read (None normally).</summary>
        SaveRecovery Recovery { get; }

        /// <summary>When the loaded save was last written (start of the absence), or null for a new game.</summary>
        System.DateTime? PreviousSessionUtc { get; }

        void Save();
    }

    /// <summary>Where the game came from when the save file was damaged.</summary>
    public enum SaveRecovery
    {
        /// <summary>The save loaded normally, or there was none yet.</summary>
        None = 0,

        /// <summary>The app stopped while saving; the finished new copy was used.</summary>
        FromUnfinishedWrite = 1,

        /// <summary>The previous save was used (a few seconds of play may be lost).</summary>
        FromBackup = 2,

        /// <summary>Nothing readable was left, so a new game started (the broken files are kept).</summary>
        NewGame = 3,
    }

    /// <summary>Upgrades raw save JSON from FromVersion to FromVersion + 1.</summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        string Migrate(string json);
    }
}
