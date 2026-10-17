using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 7 playtest: earning coins completes "Beach business" (toast, badge), the main building
    /// climbs to the Hotel, a stand reaches level 10 and ten decorations go down, real donut sales count up,
    /// the Trophies panel lists claimable goals first and pays two claims through its buttons, a friend's
    /// island visit counts, and progress plus claims survive a restart; the daily claim starts the streak goal.
    /// </summary>
    internal static partial class AchievementPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const string Key = "BeachTycoon.AutoRun.Achievements.";
        private const int DonutSalesToSee = 3;

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static AchievementService Achievements => ServiceLocator.Get<AchievementService>();
        private static AchievementBook Book => Achievements.Book;
        private static VisitService Visits => ServiceLocator.Get<VisitService>();
        private static AchievementPanelView Panel => PlaytestTools.Find<AchievementPanelView>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Buildings.MainTier == 2;
                case 3: return Buildings.MainTier == 3;
                case 4: return Buildings.MainTier == 4;
                case 5: return Progress("Donuts100") >= SessionState.GetInt(Key + "Donuts", 0) + DonutSalesToSee;
                case 6: return IslandReady();
                case 7: return SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0:
                    Time.timeScale = TestTimeScale;
                    CheckFreshStart();
                    Economy.Earn(20000);
                    Check(Book.IsComplete(Index("Coins10K")) && Book.ClaimableCount == 1, "earning 20,000 coins completes Beach business");
                    Check(Badge() == "1", "Trophies badge shows 1 reward waiting");
                    Check(Buildings.TryBuild(0), "tent started");
                    break;
                case 1:
                    Check(ToastText().Contains("Beach business"), $"toast announces the achievement ('{ToastText()}')");
                    PlaytestTools.CapturePortraitWithHud("m7_1_toast_badge.png");
                    Check(!Book.IsComplete(Index("Hotel")) && Progress("Hotel") == 1, "Hotelier tracks the tent as tier 1");
                    Check(Buildings.TryUpgrade(0), "cabin started");
                    break;
                case 2:
                case 3:
                    Check(Progress("Hotel") == Buildings.MainTier, $"Hotelier progress follows the tier ({Buildings.MainTier})");
                    Check(Buildings.TryUpgrade(0), $"tier {Buildings.MainTier + 1} started");
                    break;
                case 4:
                    Check(Book.IsComplete(Index("Hotel")), "opening the Hotel completes Hotelier");
                    Economy.Earn(500000);
                    UpgradeDonutStand(10);
                    BuyAllDecorations();
                    SessionState.SetInt(Key + "Donuts", (int)Progress("Donuts100"));
                    break;
                case 5:
                    CheckPanelAndClaims();
                    Visits.StartVisit("demo-kai");
                    break;
                case 6:
                    PlaytestTools.Click(PlaytestTools.Find<IslandHudView>(), "HomeButton");
                    break;
                case 7:
                    Check(Progress("Visits3") == 1, "visiting Kai's island counts for Globetrotter");
                    SessionState.SetInt(Key + "Claimable", Book.ClaimableCount);
                    SessionState.SetInt(Key + "Donuts", (int)Progress("Donuts100"));
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 8:
                    Time.timeScale = TestTimeScale;
                    CheckAfterRestart();
                    break;
                default:
                    AutoTestRunner.Finish();
                    break;
            }
        }

        private static bool IslandReady()
        {
            if (SceneManager.GetActiveScene().name != SceneNames.Island) return false;
            IslandSceneRoot root = PlaytestTools.Find<IslandSceneRoot>();
            return root != null && root.Layout != null;
        }

        private static int Index(string id) => Book.IndexOf(id);
        private static long Progress(string id) => Index(id) >= 0 ? Book.Progress(Index(id)) : -1;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
