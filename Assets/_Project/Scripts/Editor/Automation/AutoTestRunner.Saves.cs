using System.IO;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using BeachTycoon.Services;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Test save files: a clean start, a damaged save for the recovery test, the privacy answer, the village and a simulated absence.</summary>
    internal static partial class AutoTestRunner
    {
        private static readonly string[] SaveSuffixes = { "", ".tmp", ".bak", ".corrupt" };

        // The save and its backup, unfinished and damaged copies: a leftover backup would come back as the "new" game.
        private static void DeleteSaves()
        {
            foreach (string suffix in SaveSuffixes) File.Delete(SavePath + suffix);
        }

        /// <summary>Restarts Play mode with the main save cut in half, as if the phone died while writing it.</summary>
        public static void RestartPlayModeWithDamagedSave()
        {
            SessionState.SetBool(KeyPrefix + "DamageSave", true);
            RestartPlayMode();
        }

        private static void ApplyDamagedSave()
        {
            if (!SessionState.GetBool(KeyPrefix + "DamageSave", false)) return;
            SessionState.EraseBool(KeyPrefix + "DamageSave");
            if (!File.Exists(SavePath)) return;
            string json = File.ReadAllText(SavePath);
            File.WriteAllText(SavePath, json.Substring(0, json.Length / 2));
            Write($"Damaged the save ({json.Length / 2} of {json.Length} characters kept)");
        }

        // Older suites start as a player who already accepted, so the first-launch question stays out of their way.
        private static void AnswerPrivacy()
        {
            if (Suite == SuiteRelease || !ServiceLocator.TryGet(out PrivacyService privacy) || privacy.HasAnswered) return;
            privacy.Answer(PrivacyChoice.Accepted);
            PrivacyPanelView panel = PlaytestTools.Find<PrivacyPanelView>();
            if (panel != null && panel.IsOpen) panel.Close();
        }

        // Older suites play an already founded village and skip the greeting, so the start screen stays away.
        private static void AnswerStartScreen()
        {
            bool picksOwnCulture = Suite == SuiteStart || Suite == SuiteCulture || Suite == SuiteCharacter || Suite == SuiteAbilities;
            if (picksOwnCulture || !ServiceLocator.TryGet(out VillageService village)) return;
            StartScreenView.SkipGreetingThisRun();
            string culture = ServiceLocator.Get<GameDatabase>().cultures[0].id;
            if (!village.IsFounded) village.TryFound("Test Bay", culture, out _);
            else if (village.NeedsCulture) village.TryChooseCulture(culture, out _);
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            if (screen != null && screen.IsOpen) screen.Close();
        }

        // Snapshots written by earlier runs (own island, demo friends) start over with the save.
        private static void DeleteIslandSnapshots()
        {
            string folder = Path.Combine(Application.persistentDataPath, LocalIslandRepository.FolderName);
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        /// <summary>Shifts the save's last-seen time back before the next Play mode starts (offline earnings test).</summary>
        public static void RestartPlayModeAfterAbsence(double hours)
        {
            SessionState.SetFloat(KeyPrefix + "AbsenceHours", (float)hours);
            RestartPlayMode();
        }

        // Rewrites lastSeenUtc in the save the game just wrote on quit, as if the player had been away.
        private static void ApplySimulatedAbsence()
        {
            float hours = SessionState.GetFloat(KeyPrefix + "AbsenceHours", 0f);
            SessionState.EraseFloat(KeyPrefix + "AbsenceHours");
            if (hours <= 0f || !File.Exists(SavePath)) return;

            var data = JsonUtility.FromJson<BeachTycoon.Data.GameSaveData>(File.ReadAllText(SavePath));
            data.lastSeenUtc = System.DateTime.UtcNow.AddHours(-hours).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data));
            Write($"Simulated an absence of {hours} h");
        }
    }
}
