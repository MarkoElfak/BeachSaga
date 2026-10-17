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
    /// Writes go to a temp file first so a crash mid-write never corrupts the last good save, and the previous
    /// save is kept as a backup. A file that cannot be read is set aside (.corrupt) and the newest readable one
    /// is used instead: the save, then an unfinished write, then the backup. Autosaves every autosaveSeconds.
    /// </summary>
    public sealed class SaveService : ISaveService, ITickable
    {
        public const string FileName = "beach_save.json";
        private const string TempSuffix = ".tmp";
        private const string CorruptSuffix = ".corrupt";
        private const string BackupSuffix = ".bak";

        private readonly ITimeProvider _clock;
        private readonly SaveMigrator _migrator;
        private readonly float _autosaveSeconds;
        private float _sinceLastSave;
        private bool _mainIsGood; // only a readable save is worth keeping as the backup

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
        public string BackupPath => FilePath + BackupSuffix;
        public SaveRecovery Recovery { get; private set; }

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
                if (_mainIsGood && File.Exists(FilePath)) File.Copy(FilePath, BackupPath, true);
                File.Copy(tempPath, FilePath, true);
                File.Delete(tempPath);
                _mainIsGood = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save failed: {exception.Message}");
            }
        }

        private GameSaveData Load(long startingCoins)
        {
            string tempPath = FilePath + TempSuffix;
            bool anyFile = File.Exists(FilePath) || File.Exists(tempPath) || File.Exists(BackupPath);
            if (TryRead(FilePath, out GameSaveData data)) return Loaded(data, SaveRecovery.None, true);
            if (TryRead(tempPath, out data)) return Loaded(data, SaveRecovery.FromUnfinishedWrite, false);
            if (TryRead(BackupPath, out data)) return Loaded(data, SaveRecovery.FromBackup, false);

            if (anyFile)
            {
                Debug.LogWarning("No readable save or backup; starting a new game.");
                Recovery = SaveRecovery.NewGame;
            }

            return GameSaveData.CreateNew(startingCoins);
        }

        private GameSaveData Loaded(GameSaveData data, SaveRecovery recovery, bool fromMain)
        {
            if (recovery != SaveRecovery.None) Debug.LogWarning($"Save restored ({recovery}).");
            Recovery = recovery;
            _mainIsGood = fromMain;
            PreviousSessionUtc = ParseUtc(data.lastSeenUtc);
            LoadedFromDisk = true;
            return data;
        }

        private bool TryRead(string path, out GameSaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                string json = File.ReadAllText(path);
                var header = JsonUtility.FromJson<SaveVersionHeader>(json);
                json = _migrator.MigrateToCurrent(json, header.version);
                data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null || data.wallet == null)
                {
                    throw new InvalidDataException("Save file is empty or incomplete.");
                }

                FillMissingLists(data);
                return true;
            }
            catch (Exception exception)
            {
                // Keep the broken file for support; the caller tries the next candidate.
                Debug.LogWarning($"Could not load save {Path.GetFileName(path)} ({exception.Message}).");
                TryCopy(path, path + CorruptSuffix);
                data = null;
                return false;
            }
        }

        private static void FillMissingLists(GameSaveData data)
        {
            data.lots ??= new System.Collections.Generic.List<LotModel>();
            data.stands ??= new System.Collections.Generic.List<StandModel>();
            data.neighbours ??= new System.Collections.Generic.List<NeighbourModel>();
            data.decorations ??= new System.Collections.Generic.List<DecorationModel>();
            data.meta ??= new MetaModel();
            data.meta.settings ??= new SettingsModel();
            data.meta.dailyLastClaimDay ??= string.Empty;
            data.meta.visitBonusOwners ??= new System.Collections.Generic.List<string>();
            data.achievements ??= new System.Collections.Generic.List<AchievementModel>();
            data.miniGames ??= new System.Collections.Generic.List<MiniGameProgress>();
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
