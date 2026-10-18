using System.IO;
using UnityEditor;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The playtest suites AutoTestRunner can run, their menu items, the simulated absence and test files.</summary>
    internal static partial class AutoTestRunner
    {
        [MenuItem("Beach Tycoon/Run Playtest: Milestone 15 (camera and zoom)", priority = 27)]
        private static void RequestCamera() => File.WriteAllText(RequestPath, SuiteCamera);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 14 (cultures)", priority = 28)]
        private static void RequestCulture() => File.WriteAllText(RequestPath, SuiteCulture);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 13 (start screen)", priority = 29)]
        private static void RequestStart() => File.WriteAllText(RequestPath, SuiteStart);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 12 (release)", priority = 30)]
        private static void RequestRelease() => File.WriteAllText(RequestPath, SuiteRelease);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 11 (sound and feel)", priority = 31)]
        private static void RequestFeel() => File.WriteAllText(RequestPath, SuiteFeel);

        [MenuItem("Beach Tycoon/Run Playtest: Mini-games", priority = 32)]
        private static void RequestMiniGames() => File.WriteAllText(RequestPath, SuiteMiniGames);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 10 (new season)", priority = 33)]
        private static void RequestSeason() => File.WriteAllText(RequestPath, SuiteSeason);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 9 (day/night + events)", priority = 34)]
        private static void RequestEvents() => File.WriteAllText(RequestPath, SuiteEvents);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 8 (managers)", priority = 35)]
        private static void RequestManagers() => File.WriteAllText(RequestPath, SuiteManagers);

        [MenuItem("Beach Tycoon/Run Playtest: Polish (queues, rotation, windows)", priority = 36)]
        private static void RequestPolish() => File.WriteAllText(RequestPath, SuitePolish);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 7 (achievements)", priority = 37)]
        private static void RequestAchievements() => File.WriteAllText(RequestPath, SuiteAchievements);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 6 (visiting)", priority = 38)]
        private static void RequestOnline() => File.WriteAllText(RequestPath, SuiteOnline);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 4 (move + performance)", priority = 39)]
        private static void RequestMove() => File.WriteAllText(RequestPath, SuiteMove);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 3 (meta)", priority = 40)]
        private static void RequestMeta() => File.WriteAllText(RequestPath, SuiteMeta);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 2 (island)", priority = 41)]
        private static void RequestIsland() => File.WriteAllText(RequestPath, SuiteIsland);

        private const string SuiteIsland = "m2";
        private const string SuiteMeta = "m3";
        private const string SuiteMove = "m4";
        private const string SuiteOnline = "m6";
        private const string SuiteAchievements = "m7";
        private const string SuitePolish = "polish";
        private const string SuiteManagers = "m8";
        private const string SuiteEvents = "m9";
        private const string SuiteSeason = "m10";
        private const string SuiteMiniGames = "games";
        private const string SuiteFeel = "feel";
        private const string SuiteRelease = "release";
        private const string SuiteStart = "start";
        private const string SuiteCulture = "culture";
        private const string SuiteCamera = "camera";

        private static bool IsKnownSuite(string suite) =>
            suite == SuiteIsland || suite == SuiteMove || suite == SuiteOnline || suite == SuiteAchievements || suite == SuitePolish
            || suite == SuiteManagers || suite == SuiteEvents || suite == SuiteSeason || suite == SuiteMiniGames
            || suite == SuiteFeel || suite == SuiteRelease || suite == SuiteStart || suite == SuiteCulture
            || suite == SuiteCamera;

        private static string Suite => SessionState.GetString(KeyPrefix + "Suite", SuiteMeta);

        private static string SuiteTitle
        {
            get
            {
                if (Suite == SuiteIsland) return "M2 island";
                if (Suite == SuiteOnline) return "M6 visiting";
                if (Suite == SuiteAchievements) return "M7 achievements";
                if (Suite == SuitePolish) return "Polish";
                if (Suite == SuiteManagers) return "M8 managers";
                if (Suite == SuiteEvents) return "M9 day/night + events";
                if (Suite == SuiteSeason) return "M10 new season";
                if (Suite == SuiteMiniGames) return "Mini-games";
                if (Suite == SuiteFeel) return "M11 sound and feel";
                if (Suite == SuiteRelease) return "M12 release";
                if (Suite == SuiteStart) return "M13 start screen";
                if (Suite == SuiteCulture) return "M14 cultures";
                if (Suite == SuiteCamera) return "M15 camera";
                return Suite == SuiteMove ? "M4 move + performance" : "M3 meta";
            }
        }

        private static bool IsStepReady(int step)
        {
            if (Suite == SuiteIsland) return PlaytestSteps.IsReady(step);
            if (Suite == SuiteOnline) return OnlinePlaytestSteps.IsReady(step);
            if (Suite == SuiteAchievements) return AchievementPlaytestSteps.IsReady(step);
            if (Suite == SuitePolish) return PolishPlaytestSteps.IsReady(step);
            if (Suite == SuiteManagers) return ManagerPlaytestSteps.IsReady(step);
            if (Suite == SuiteEvents) return EventPlaytestSteps.IsReady(step);
            if (Suite == SuiteSeason) return SeasonPlaytestSteps.IsReady(step);
            if (Suite == SuiteMiniGames) return MiniGamePlaytestSteps.IsReady(step);
            if (Suite == SuiteFeel) return FeelPlaytestSteps.IsReady(step);
            if (Suite == SuiteRelease) return ReleasePlaytestSteps.IsReady(step);
            if (Suite == SuiteStart) return StartPlaytestSteps.IsReady(step);
            if (Suite == SuiteCulture) return CulturePlaytestSteps.IsReady(step);
            if (Suite == SuiteCamera) return CameraPlaytestSteps.IsReady(step);
            return Suite == SuiteMove ? MovePlaytestSteps.IsReady(step) : MetaPlaytestSteps.IsReady(step);
        }

        private static void RunStep(int step)
        {
            if (Suite == SuiteIsland) PlaytestSteps.Run(step);
            else if (Suite == SuiteOnline) OnlinePlaytestSteps.Run(step);
            else if (Suite == SuiteAchievements) AchievementPlaytestSteps.Run(step);
            else if (Suite == SuitePolish) PolishPlaytestSteps.Run(step);
            else if (Suite == SuiteManagers) ManagerPlaytestSteps.Run(step);
            else if (Suite == SuiteEvents) EventPlaytestSteps.Run(step);
            else if (Suite == SuiteSeason) SeasonPlaytestSteps.Run(step);
            else if (Suite == SuiteMiniGames) MiniGamePlaytestSteps.Run(step);
            else if (Suite == SuiteFeel) FeelPlaytestSteps.Run(step);
            else if (Suite == SuiteRelease) ReleasePlaytestSteps.Run(step);
            else if (Suite == SuiteStart) StartPlaytestSteps.Run(step);
            else if (Suite == SuiteCulture) CulturePlaytestSteps.Run(step);
            else if (Suite == SuiteCamera) CameraPlaytestSteps.Run(step);
            else if (Suite == SuiteMove) MovePlaytestSteps.Run(step);
            else MetaPlaytestSteps.Run(step);
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
