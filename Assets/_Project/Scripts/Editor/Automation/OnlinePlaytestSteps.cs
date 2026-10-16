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
    /// Milestone 6 playtest: builds a small beach, opens the Visit list from the HUD, visits the player's own
    /// saved snapshot (read-only: same objects, taps change nothing, bonus paid), goes home (beach restored),
    /// visits a friend (like, gift, crowd), visits again (no second bonus), a bigger friend's island, restarts
    /// (player id and bonus record kept) and tries a missing island (stays home).
    /// </summary>
    internal static partial class OnlinePlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const long TestGrant = 5000000;
        private const string Key = "BeachTycoon.AutoRun.Online.";
        private const string Kai = "demo-kai";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static VisitService Visits => ServiceLocator.Get<VisitService>();
        private static ISocialService Social => ServiceLocator.Get<ISocialService>();
        private static MockAnalyticsService Analytics => (MockAnalyticsService)ServiceLocator.Get<IAnalyticsService>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Buildings.MainTier == 2;
                case 3: case 5: case 7: case 9: return IslandReady();
                case 4: case 6: case 8: case 10: return BeachReady();
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0:
                    Time.timeScale = TestTimeScale;
                    FinishTutorial();
                    Economy.Earn(TestGrant);
                    Check(Buildings.TryBuild(0), "tent started");
                    break;
                case 1:
                    Check(Buildings.TryUpgrade(0), "cabin started");
                    BuyDecorations(3);
                    break;
                case 2:
                    PlaytestTools.ClickProduct(1);
                    RememberHome();
                    OpenVisitPanelFromHud();
                    VisitRow(0);
                    break;
                case 3:
                    CheckOwnIsland();
                    PlaytestTools.Click(PlaytestTools.Find<IslandHudView>(), "HomeButton");
                    break;
                case 4:
                    CheckHomeRestored();
                    OpenVisitPanelFromHud();
                    VisitRow(2);
                    break;
                case 5:
                    CheckFriendIsland();
                    break;
                case 6:
                    Check(!Visits.CanClaimBonus(Kai), "Kai's bonus is used up for today");
                    OpenVisitPanelFromHud();
                    VisitRow(2);
                    break;
                case 7:
                    Check(Visits.LastBonus == 0 && Text("Banner/Bonus").Contains("already"), "second visit the same day pays no bonus");
                    Check(Text("LikeButton/Label") == "Liked!", "like is remembered on the second visit");
                    PlaytestTools.Click(PlaytestTools.Find<IslandHudView>(), "HomeButton");
                    break;
                case 8:
                    OpenVisitPanelFromHud();
                    VisitRow(3);
                    break;
                case 9:
                    CheckBigIsland();
                    break;
                case 10:
                    Check(!PlaytestTools.Find<DailyRewardPanelView>().IsOpen, "daily calendar does not pop up again after a visit");
                    SessionState.SetString(Key + "Player", Visits.PlayerId);
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 11:
                    Time.timeScale = TestTimeScale;
                    PlaytestTools.Find<DailyRewardPanelView>().Close();
                    Check(Visits.PlayerId == SessionState.GetString(Key + "Player", "?"), "player id survives a restart");
                    Check(!Visits.CanClaimBonus(Kai), "visit bonus record survives a restart");
                    Visits.StartVisit("nobody-here");
                    break;
                case 12:
                    Check(SceneManager.GetActiveScene().name == SceneNames.Beach && !Visits.IsVisiting, "a missing island keeps the player at home");
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

        private static bool BeachReady() =>
            SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
