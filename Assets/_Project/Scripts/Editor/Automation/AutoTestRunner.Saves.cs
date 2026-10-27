using System.IO;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using UnityEditor;

namespace BeachTycoon.EditorTools
{
    /// <summary>Test save files: a clean start, a damaged save for the recovery test, the privacy answer and the village.</summary>
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
            if (Suite == SuiteStart || Suite == SuiteCulture || Suite == SuiteCharacter || !ServiceLocator.TryGet(out VillageService village)) return;
            StartScreenView.SkipGreetingThisRun();
            string culture = ServiceLocator.Get<GameDatabase>().cultures[0].id;
            if (!village.IsFounded) village.TryFound("Test Bay", culture, out _);
            else if (village.NeedsCulture) village.TryChooseCulture(culture, out _);
            StartScreenView screen = PlaytestTools.Find<StartScreenView>();
            if (screen != null && screen.IsOpen) screen.Close();
        }
    }
}
