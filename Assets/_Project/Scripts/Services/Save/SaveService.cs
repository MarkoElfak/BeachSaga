using System;
using System.Globalization;
using System.IO;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Loads and saves GameSaveData as JSON in a directory (Application.persistentDataPath in the game).
    /// Writes go to a temp file first so a crash mid-write never corrupts the last good save.
    /// Autosaves every autosaveSeconds when ticked.
    /// </summary>
    public sealed class SaveService : ISaveService, ITickable
    {
        public const string FileName = "beach_save.json";
        private const string TempSuffix = ".tmp";
        private const string CorruptSuffix = ".corrupt";

        private readonly ITimeProvider _clock;
        private readonly SaveMigrator _migrator;
        private readonly float _autosaveSeconds;
        private float _sinceLastSave;

        public SaveService(string directory, float autosaveSeconds, long startingCoins, ITimeProvider clock,
            SaveMigrator migrator = null)
        {
            FilePath = Path.Combine(directory, FileName);
            _autosaveSeconds = autosaveSeconds;
            _clock = clock;
            _migrator = migrator ?? new SaveMigrator(SaveMigrations.All);
            Data = Load(startingCoins);
        }

        public GameSaveData Data { get; private set; }
        public bool LoadedFromDisk { get; private set; }
        public string FilePath { get; }

        /// <summary>Last-seen time from the loaded file (before this session saved anything), if any.</summary>
        public DateTime? PreviousSessionUtc { get; private set; }

        public void Tick(float deltaTime)
        {
            _sinceLastSave += deltaTime;
            if (_sinceLastSave >= _autosaveSeconds)
            {
                Save();
            }
        }

        public void Save()
        {
            _sinceLastSave = 0f;
            Data.version = GameSaveData.CurrentVersion;
            Data.lastSeenUtc = _clock.UtcNow.ToString("o", CultureInfo.InvariantCulture);

            string tempPath = FilePath + TempSuffix;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? ".");
                File.WriteAllText(tempPath, JsonUtility.ToJson(Data));
                File.Copy(tempPath, FilePath, true);
                File.Delete(tempPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save failed: {exception.Message}");
            }
        }

        private GameSaveData Load(long startingCoins)
        {
            if (!File.Exists(FilePath))
            {
                return GameSaveData.CreateNew(startingCoins);
            }

            try
            {
                string json = File.ReadAllText(FilePath);
                var header = JsonUtility.FromJson<SaveVersionHeader>(json);
                json = _migrator.MigrateToCurrent(json, header.version);
                var data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null || data.wallet == null)
                {
                    throw new InvalidDataException("Save file is empty or incomplete.");
                }

                data.lots ??= new System.Collections.Generic.List<LotModel>();
                data.stands ??= new System.Collections.Generic.List<StandModel>();
                data.neighbours ??= new System.Collections.Generic.List<NeighbourModel>();
                data.decorations ??= new System.Collections.Generic.List<DecorationModel>();
                data.meta ??= new MetaModel();
                data.meta.settings ??= new SettingsModel();
                data.meta.dailyLastClaimDay ??= string.Empty;
                PreviousSessionUtc = ParseUtc(data.lastSeenUtc);
                LoadedFromDisk = true;
                return data;
            }
            catch (Exception exception)
            {
                // Keep the broken file for support, then start fresh rather than crash.
                Debug.LogWarning($"Could not load save ({exception.Message}); starting a new game.");
                TryCopy(FilePath, FilePath + CorruptSuffix);
                return GameSaveData.CreateNew(startingCoins);
            }
        }

        private static void TryCopy(string from, string to)
        {
            try
            {
                File.Copy(from, to, true);
            }
            catch (IOException)
            {
                // Best effort only.
            }
        }

        private static DateTime? ParseUtc(string value)
        {
            // RoundtripKind cannot be combined with AdjustToUniversal, so convert afterwards.
            bool parsed = DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out DateTime time);
            return parsed ? time.ToUniversalTime() : (DateTime?)null;
        }
    }
}
