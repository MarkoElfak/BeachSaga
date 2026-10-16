using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 8 playtest: a stand's coins wait for a tap until a manager is hired from the stand's upgrade
    /// panel; then they collect themselves (coins already waiting too), the stand wears a hat, Delegator counts
    /// it, and after a restart the manager, the hat and the higher offline efficiency are still there.
    /// </summary>
    internal static class ManagerPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const string Key = "BeachTycoon.AutoRun.Managers.";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static CoinSpawner Coins => PlaytestTools.Find<CoinSpawner>();
        private static UpgradePanelView Panel => PlaytestTools.Find<UpgradePanelView>();
        private static StandController Donuts => Stands.FindByProduct("donuts");

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Coins.CountWaiting(Donuts.Index) >= 2;
                case 3: return Panel.IsOpen;
                case 4: return Economy.Coins > SessionState.GetInt(Key + "AfterHire", 0) && Coins.CountWaiting(Donuts.Index) == 0;
                case 5: return SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;
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
                    Check(!Donuts.Model.manager && !Hat().activeSelf, "a new donut stand has no manager and no hat");
                    break;
                case 2:
                    Check(Coins.CountWaiting(Donuts.Index) >= 2, $"without a manager coins wait for a tap ({Coins.CountWaiting(Donuts.Index)} waiting)");
                    OpenStandPanel();
                    break;
                case 3:
                    Hire();
                    break;
                case 4:
                    Check(true, $"managed coins collect themselves ({Economy.Coins} coins, none waiting)");
                    Check(Book.Progress(Book.IndexOf("Managers3")) == 1, "Delegator counts the first manager");
                    PlaytestTools.FocusCamera(PlaytestTools.Find<StandView>().transform.position, 12f);
                    PlaytestTools.CapturePortrait("m8_2_manager_hat.png");
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 5:
                    Check(Donuts.Model.manager && Hat().activeSelf, "the manager and the hat survive a restart");
                    EconomyConfig economy = ServiceLocator.Get<GameDatabase>().economy;
                    float efficiency = ServiceLocator.Get<OfflineEarningsService>().Efficiency;
                    Check(Mathf.Approximately(efficiency, Mathf.Min(economy.offlineEfficiencyMax, economy.offlineEfficiency + economy.offlineEfficiencyPerManager)),
                        $"offline efficiency rises with the manager ({economy.offlineEfficiency:0.00} -> {efficiency:0.00})");
                    break;
                default:
                    AutoTestRunner.Finish();
                    break;
            }
        }

        // Tap the stand and open its panel from the floating Upgrade button (captured with the panel open).
        private static void OpenStandPanel()
        {
            StandView view = PlaytestTools.Find<StandView>();
            Economy.Earn(Donuts.Product.managerCost);
            PlaytestTools.TapAndUpgrade(view.transform.position + Vector3.up * 0.8f);
            Check(Panel.IsOpen && Panel.Stand == Donuts, "the stand's Upgrade button opens its own panel");
            Check(HireLabel().Contains("Hire manager"), $"the panel offers a manager ('{HireLabel()}')");
            PlaytestTools.Capture("m8_1_hire_panel.png");
        }

        private static void Hire()
        {
            long cost = Donuts.Product.managerCost;
            long before = Economy.Coins;
            PlaytestTools.Click(Panel, "HireButton");
            Check(Donuts.Model.manager && Economy.Coins == before - cost, $"hiring costs {cost} and is saved on the stand");
            Check(Hat().activeSelf, "the stand now wears the manager's hat");
            Check(HireLabel() == "Manager hired" && !HireButton().interactable, "the panel shows the manager as hired");
            Check(Coins.CountWaiting(Donuts.Index) == 0, "the manager picks up the coins that were waiting");
            Check(((MockAnalyticsService)ServiceLocator.Get<IAnalyticsService>()).HasLogged(AnalyticsEvents.ManagerHired), "analytics: manager_hired");
            Panel.Close();
            SessionState.SetInt(Key + "AfterHire", (int)Economy.Coins);
        }

        private static AchievementBook Book => ServiceLocator.Get<AchievementService>().Book;
        private static GameObject Hat() => PlaytestTools.Find<StandView>().transform.Find(StandPrefabs.ManagerHatName).gameObject;
        private static Button HireButton() => Panel.transform.Find("Root/Window/HireButton").GetComponent<Button>();
        private static string HireLabel() => HireButton().GetComponentInChildren<TMP_Text>(true).text;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
