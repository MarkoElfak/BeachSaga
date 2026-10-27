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
    /// Milestone 10 playtest: plays a full season (earn, every tier up to the Resort, all stands, a worker,
    /// decorations and a neighbour), opens the New season panel from the HUD and starts a new season. The game
    /// reboots: coins, the main building, the stands and the staff start over, seashells are paid, neighbours and
    /// decorations stay, and a real sale, rent and the panel all include the seashell bonus. A restart keeps it.
    /// </summary>
    internal static partial class SeasonPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const long SeasonCoins = 2000000;
        private const string Key = "BeachTycoon.AutoRun.Season.";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static NeighbourService Neighbours => ServiceLocator.Get<NeighbourService>();
        private static DecorationService Decorations => ServiceLocator.Get<DecorationService>();
        private static SeasonService Season => ServiceLocator.Get<SeasonService>();
        private static SeasonPanelView Panel => PlaytestTools.Find<SeasonPanelView>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Buildings.MainTier == 2;
                case 3: return Buildings.MainTier == 3;
                case 4: return Buildings.MainTier == 4;
                case 5: return Buildings.MainTier == 5 && Stands.FindByProduct("drinks") != null;
                case 6: return Neighbours.Count > 0 && Buildings.GetLot(Neighbours.Get(0).lotIndex).state == LotState.Built;
                case 7: return Panel != null && Panel.IsOpen;
                case 8: return Rebooted();
                case 9: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 10: return _saleChecked && _rentChecked;
                case 11: return SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0:
                    Time.timeScale = TestTimeScale;
                    ResetChecks();
                    MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
                    meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
                    EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
                    Check(Season.Season == 1 && Season.Seashells == 0 && Mathf.Approximately(Season.IncomeMultiplier, 1f), "a new game is season 1 with no seashells");
                    Check(!Panel.IsButtonShown, "no New season button before the Resort");
                    Economy.Earn(SeasonCoins);
                    Check(Buildings.TryBuild(0), "tent started");
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                    UnlockProducts();
                    Check(Buildings.TryUpgrade(0), $"tier {Buildings.MainTier + 1} started");
                    break;
                case 5:
                    PlaySeason();
                    break;
                case 6:
                    OpenPanel();
                    break;
                case 7:
                    StartNewSeason();
                    break;
                case 8:
                    CheckReset();
                    Check(Buildings.TryBuild(0), "the tent can be built again");
                    break;
                case 9:
                    WatchIncome();
                    break;
                case 10:
                    CheckIncome();
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 11:
                    CheckAfterRestart();
                    break;
                default:
                    EventBus.Unsubscribe<SaleCompleted>(OnSale);
                    EventBus.Unsubscribe<RentDue>(OnRent);
                    AutoTestRunner.Finish();
                    break;
            }
        }

        private static void UnlockProducts()
        {
            foreach (ProductDefinition product in ServiceLocator.Get<GameDatabase>().products)
            {
                if (Stands.CanUnlock(product)) Stands.TryUnlockProduct(product);
            }
        }

        // Everything a season builds up: all stands, a worker, decorations and a neighbour moving in.
        private static void PlaySeason()
        {
            UnlockProducts();
            StandController donuts = Stands.FindByProduct("donuts");
            while (donuts.Model.level < 5 && Stands.TryUpgradeStand(donuts)) { }
            var staff = ServiceLocator.Get<StaffService>();
            Check(staff.Seekers.Count > 0 && staff.TryHire(staff.Seekers[0].Id, Stands.FindByProduct("drinks").Index), "a worker hired for drinks");
            foreach (DecorationDefinition decoration in ServiceLocator.Get<GameDatabase>().decorations)
            {
                if (Decorations.CanBuy(decoration)) Decorations.TryBuy(decoration);
            }

            for (int i = 0; i < 200 && !Neighbours.HasPendingRequest; i++)
            {
                EventBus.Publish(new VisitorPurchased(0, ServiceLocator.Get<GameDatabase>().neighbours.minPurchasesToAsk));
            }

            Check(Neighbours.Accept(), "a neighbour moved in");
            Check(Stands.Count == 5 && Decorations.Count > 0, $"season 1 has {Stands.Count} stands and {Decorations.Count} decorations");
        }

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
