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
    /// Milestone 8 playtest, reworked for staff (M18): a stand's coins wait for a tap until a level 3 worker runs it.
    /// A job seeker is hired from their card, reaches level 3, then the coins collect themselves (those waiting too),
    /// the stand wears the hat, Delegator counts it, and after a restart the worker, the hat and the higher offline
    /// efficiency are still there.
    /// </summary>
    internal static class ManagerPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const string Key = "BeachTycoon.AutoRun.Managers.";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static StaffService Staff => ServiceLocator.Get<StaffService>();
        private static CoinSpawner Coins => PlaytestTools.Find<CoinSpawner>();
        private static WorkerCardView Card => PlaytestTools.Find<WorkerCardView>();
        private static int Target => SessionState.GetInt(Key + "Stand", -1);
        private static StaffMember Worker => Staff.WorkerAt(Target);

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Buildings.MainTier == 2 && Buildings.GetLot(0).state == LotState.Built && Staff.Seekers.Count > 0;
                case 3: return FreeStand() >= 0;
                case 4: return Coins.CountWaiting(Target) >= 2;
                case 5:
                    return Worker != null && Worker.IsWorking && Worker.Model.level >= 3
                        && Economy.Coins > SessionState.GetInt(Key + "AfterHire", 0) && Coins.CountWaiting(Target) == 0;
                case 6: return SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0:
                    Time.timeScale = TestTimeScale;
                    MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
                    meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
                    EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
                    Check(Buildings.TryBuild(0), "tent started");
                    break;
                case 1:
                    Check(Staff.Slots == 0, "the tent has no staff slots");
                    Economy.Earn(ServiceLocator.Get<GameDatabase>().GetTier(2).upgradeCost);
                    Check(Buildings.TryUpgrade(0), "cabin started (two staff slots)");
                    break;
                case 2:
                    // The donut stand may be the character's: then the towel stand is the one to staff.
                    if (FreeStand() >= 0) break;
                    Economy.Earn(100000);
                    CharacterService character = ServiceLocator.Get<CharacterService>();
                    Check(Stands.TryUnlockProduct(ServiceLocator.Get<GameDatabase>().FindProduct("towels")),
                        $"towel stand opened ({Stands.Count} stands, {character.Character?.displayName ?? "no character"} at stand {character.StandIndex})");
                    break;
                case 3:
                    SessionState.SetInt(Key + "Stand", FreeStand());
                    StandController stand = Stands.Get(Target);
                    Check(!stand.Model.manager && !Hat(Target).activeSelf, $"{stand.Product.id}: nobody collects, no hat");
                    Check(Mathf.Approximately(stand.ServeSpeed, Staff.Config.unstaffedServeSpeed), $"an empty stand serves at half speed ({stand.ServeSpeed:0.00})");
                    break;
                case 4:
                    Check(Coins.CountWaiting(Target) >= 2, $"without a level 3 worker coins wait for a tap ({Coins.CountWaiting(Target)} waiting)");
                    Hire();
                    break;
                case 5:
                    Check(Stands.Get(Target).Model.manager && Hat(Target).activeSelf, "at level 3 the worker collects the coins and the stand wears the hat");
                    Check(Book.Progress(Book.IndexOf("Managers3")) == 1, "Delegator counts the first level 3 worker");
                    Check(((MockAnalyticsService)ServiceLocator.Get<IAnalyticsService>()).HasLogged(AnalyticsEvents.ManagerHired), "analytics: manager_hired");
                    PlaytestTools.FocusCamera(StandViewOf(Target).transform.position, 12f);
                    PlaytestTools.CapturePortrait("m8_2_manager_hat.png");
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 6:
                    Check(Worker != null && Worker.IsWorking && Stands.Get(Target).Model.manager && Hat(Target).activeSelf,
                        "the worker, the coin collecting and the hat survive a restart");
                    EconomyConfig economy = ServiceLocator.Get<GameDatabase>().economy;
                    float efficiency = ServiceLocator.Get<OfflineEarningsService>().Efficiency;
                    Check(Mathf.Approximately(efficiency, Mathf.Min(economy.offlineEfficiencyMax, economy.offlineEfficiency + economy.offlineEfficiencyPerManager)),
                        $"offline efficiency rises with them ({economy.offlineEfficiency:0.00} -> {efficiency:0.00})");
                    break;
                default:
                    AutoTestRunner.Finish();
                    break;
            }
        }

        // Opens a job seeker's card (as a tap on them does) and hires them for the target stand from it.
        private static void Hire()
        {
            StaffMember seeker = Staff.Seekers[0];
            EventBus.Publish(new WorkerCardRequested(seeker.Id));
            Check(Card.IsOpen && Card.WorkerId == seeker.Id, $"tapping a job seeker opens their card ({Card.NameText})");
            PlaytestTools.Capture("m8_1_hire_card.png");
            Card.StandButton(Target).onClick.Invoke();
            Check(!Card.IsOpen && Worker == seeker, $"hired {seeker.Model.name} for the stand");
            Check(!Stands.Get(Target).Model.manager, "a new worker does not collect coins yet");

            // Skip ahead to one sale before level 3, so the next sale at the stand makes them a collector.
            seeker.Model.level = 2;
            seeker.Model.xp = Staff.SalesToNextLevel(seeker.Model) - 1;
            SessionState.SetInt(Key + "AfterHire", (int)Economy.Coins);
        }

        private static int FreeStand()
        {
            for (int i = 0; i < Stands.Count; i++) if (Staff.IsStandFree(i) && StandViewOf(i) != null) return i;
            return -1;
        }

        private static StandView StandViewOf(int index)
        {
            foreach (StandView view in Object.FindObjectsByType<StandView>())
            {
                if (view.Stand != null && view.Stand.Index == index) return view;
            }

            return null;
        }

        private static AchievementBook Book => ServiceLocator.Get<AchievementService>().Book;
        private static GameObject Hat(int index) => StandViewOf(index).transform.Find(StandPrefabs.ManagerHatName).gameObject;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
