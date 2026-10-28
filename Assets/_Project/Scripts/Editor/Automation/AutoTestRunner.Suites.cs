using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace BeachTycoon.EditorTools
{
    /// <summary>The playtest suites AutoTestRunner can run, their menu items, the simulated absence and test files.</summary>
    internal static partial class AutoTestRunner
    {
        [MenuItem("Beach Tycoon/Run Playtest: Milestone 22 (device check)", priority = 20)]
        private static void RequestDevice() => File.WriteAllText(RequestPath, SuiteDevice);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 21 (economy balance)", priority = 21)]
        private static void RequestEconomy() => File.WriteAllText(RequestPath, SuiteEconomy);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 20 (village buildings)", priority = 22)]
        private static void RequestVillage() => File.WriteAllText(RequestPath, SuiteVillage);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 19 (country abilities)", priority = 23)]
        private static void RequestAbilities() => File.WriteAllText(RequestPath, SuiteAbilities);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 18 (hiring staff)", priority = 24)]
        private static void RequestStaff() => File.WriteAllText(RequestPath, SuiteStaff);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 17 (your character)", priority = 25)]
        private static void RequestCharacter() => File.WriteAllText(RequestPath, SuiteCharacter);

        [MenuItem("Beach Tycoon/Run Playtest: Milestone 16 (bigger map under fog)", priority = 26)]
        private static void RequestMap() => File.WriteAllText(RequestPath, SuiteMap);

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
        private const string SuiteMap = "map";
        private const string SuiteCharacter = "character";
        private const string SuiteStaff = "staff";
        private const string SuiteAbilities = "abilities";
        private const string SuiteVillage = "village";
        private const string SuiteEconomy = "economy";
        private const string SuiteDevice = "device";

        /// <summary>A suite's title in the log, whether a step may run yet, and the step itself.</summary>
        private readonly struct SuiteSteps
        {
            public readonly string Title;
            public readonly Func<int, bool> IsReady;
            public readonly Action<int> Run;

            public SuiteSteps(string title, Func<int, bool> isReady, Action<int> run)
            {
                Title = title;
                IsReady = isReady;
                Run = run;
            }
        }

        private static readonly Dictionary<string, SuiteSteps> Suites = new Dictionary<string, SuiteSteps>
        {
            { SuiteIsland, new SuiteSteps("M2 island", PlaytestSteps.IsReady, PlaytestSteps.Run) },
            { SuiteMeta, new SuiteSteps("M3 meta", MetaPlaytestSteps.IsReady, MetaPlaytestSteps.Run) },
            { SuiteMove, new SuiteSteps("M4 move + performance", MovePlaytestSteps.IsReady, MovePlaytestSteps.Run) },
            { SuiteOnline, new SuiteSteps("M6 visiting", OnlinePlaytestSteps.IsReady, OnlinePlaytestSteps.Run) },
            { SuiteAchievements, new SuiteSteps("M7 achievements", AchievementPlaytestSteps.IsReady, AchievementPlaytestSteps.Run) },
            { SuitePolish, new SuiteSteps("Polish", PolishPlaytestSteps.IsReady, PolishPlaytestSteps.Run) },
            { SuiteManagers, new SuiteSteps("M8 managers", ManagerPlaytestSteps.IsReady, ManagerPlaytestSteps.Run) },
            { SuiteEvents, new SuiteSteps("M9 day/night + events", EventPlaytestSteps.IsReady, EventPlaytestSteps.Run) },
            { SuiteSeason, new SuiteSteps("M10 new season", SeasonPlaytestSteps.IsReady, SeasonPlaytestSteps.Run) },
            { SuiteMiniGames, new SuiteSteps("Mini-games", MiniGamePlaytestSteps.IsReady, MiniGamePlaytestSteps.Run) },
            { SuiteFeel, new SuiteSteps("M11 sound and feel", FeelPlaytestSteps.IsReady, FeelPlaytestSteps.Run) },
            { SuiteRelease, new SuiteSteps("M12 release", ReleasePlaytestSteps.IsReady, ReleasePlaytestSteps.Run) },
            { SuiteStart, new SuiteSteps("M13 start screen", StartPlaytestSteps.IsReady, StartPlaytestSteps.Run) },
            { SuiteCulture, new SuiteSteps("M14 cultures", CulturePlaytestSteps.IsReady, CulturePlaytestSteps.Run) },
            { SuiteCamera, new SuiteSteps("M15 camera", CameraPlaytestSteps.IsReady, CameraPlaytestSteps.Run) },
            { SuiteMap, new SuiteSteps("M16 map", MapPlaytestSteps.IsReady, MapPlaytestSteps.Run) },
            { SuiteCharacter, new SuiteSteps("M17 character", CharacterPlaytestSteps.IsReady, CharacterPlaytestSteps.Run) },
            { SuiteStaff, new SuiteSteps("M18 staff", StaffPlaytestSteps.IsReady, StaffPlaytestSteps.Run) },
            { SuiteAbilities, new SuiteSteps("M19 abilities", AbilityPlaytestSteps.IsReady, AbilityPlaytestSteps.Run) },
            { SuiteVillage, new SuiteSteps("M20 village", VillagePlaytestSteps.IsReady, VillagePlaytestSteps.Run) },
            { SuiteEconomy, new SuiteSteps("M21 economy", EconomyPlaytestSteps.IsReady, EconomyPlaytestSteps.Run) },
            { SuiteDevice, new SuiteSteps("M22 device", DevicePlaytestSteps.IsReady, DevicePlaytestSteps.Run) },
        };

        private static bool IsKnownSuite(string suite) => Suites.ContainsKey(suite);

        private static string Suite => SessionState.GetString(KeyPrefix + "Suite", SuiteMeta);

        private static SuiteSteps Current => Suites.TryGetValue(Suite, out SuiteSteps steps) ? steps : Suites[SuiteMeta];

        private static string SuiteTitle => Current.Title;

        private static bool IsStepReady(int step) => Current.IsReady(step);

        private static void RunStep(int step) => Current.Run(step);
    }
}
