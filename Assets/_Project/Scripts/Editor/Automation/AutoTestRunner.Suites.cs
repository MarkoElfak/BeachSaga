using System.IO;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The playtest suites AutoTestRunner can run, their menu items, and the simulated absence.</summary>
    internal static partial class AutoTestRunner
    {
        [MenuItem("Beach Tycoon/Run Playtest: Milestone 4 (move + performance)", priority = 39)]
        private static void RequestMove() => File.WriteAllText(RequestPath, SuiteMove);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 3 (meta)", priority = 40)]
        private static void RequestMeta() => File.WriteAllText(RequestPath, SuiteMeta);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 2 (island)", priority = 41)]
        private static void RequestIsland() => File.WriteAllText(RequestPath, SuiteIsland);

        private const string SuiteIsland = "m2";
        private const string SuiteMeta = "m3";
        private const string SuiteMove = "m4";

        private static string Suite => SessionState.GetString(KeyPrefix + "Suite", SuiteMeta);

        private static string SuiteTitle => Suite == SuiteIsland ? "M2 island" : Suite == SuiteMove ? "M4 move + performance" : "M3 meta";

        private static bool IsStepReady(int step)
        {
            if (Suite == SuiteIsland) return PlaytestSteps.IsReady(step);
            return Suite == SuiteMove ? MovePlaytestSteps.IsReady(step) : MetaPlaytestSteps.IsReady(step);
        }

        private static void RunStep(int step)
        {
            if (Suite == SuiteIsland) PlaytestSteps.Run(step);
            else if (Suite == SuiteMove) MovePlaytestSteps.Run(step);
            else MetaPlaytestSteps.Run(step);
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
