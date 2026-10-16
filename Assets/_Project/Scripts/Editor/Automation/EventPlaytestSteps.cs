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
    /// Milestone 9 playtest: grows the beach to the Hotel with every stand, measures a quiet stretch, then
    /// forces each beach event and checks its effect (heatwave: drinks pay 1.5x and get a bigger share of the
    /// orders; tourist bus: a crowd and faster arrivals; surf contest: lessons pay 2x), the banner and
    /// countdown, the day and night colours, and that the time of day survives a restart while events do not.
    /// </summary>
    internal static partial class EventPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const int MinOrders = 15;
        private const float Morning = 0.2f;
        private const string Key = "BeachTycoon.AutoRun.Events.";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static VisitorService Visitors => ServiceLocator.Get<VisitorService>();
        private static BeachEventService Events => ServiceLocator.Get<BeachEventService>();
        private static DayClock Clock => ServiceLocator.Get<DayClock>();
        private static BeachEventBannerView Banner => PlaytestTools.Find<BeachEventBannerView>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Buildings.MainTier == 2;
                case 3: return Buildings.MainTier == 3;
                case 4: return Buildings.MainTier == 4 && Stands.FindByProduct("drinks") != null;
                case 5: return ShowsPhase(Morning);
                case 6: return Total() - _baseTotal >= MinOrders;
                case 7: return _saleChecked && Total() - _heatTotal >= MinOrders;
                case 8: return !Events.IsActive;
                case 9: return _saleChecked;
                case 10: return ShowsPhase(0.25f);
                case 11: return ShowsPhase(0.75f);
                case 12: return Clock.Phase > SessionState.GetFloat(Key + "Phase", 1f) + 0.002f;
                case 13: return SceneManager.GetActiveScene().name == SceneNames.Beach && PlaytestTools.Find<StandView>() != null;
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
                    Check(!Events.IsActive && !Banner.IsShowing && Events.SecondsUntilNext > 0f, $"no event at the start (first in {Events.SecondsUntilNext:0} s)");
                    Clock.SetPhase(0.25f);
                    Economy.Earn(500000);
                    Check(Buildings.TryBuild(0), "tent started");
                    break;
                case 1:
                case 2:
                    Check(Buildings.TryUpgrade(0), $"tier {Buildings.MainTier + 1} started");
                    break;
                case 3:
                    UnlockProducts();
                    Check(Buildings.TryUpgrade(0), "hotel started");
                    break;
                case 4:
                    UnlockProducts();
                    if (Events.IsActive) Events.End(); // a random event may have started while building
                    Clock.SetPhase(Morning); // daylight for the screenshots
                    break;
                case 5:
                    StartBaseline();
                    break;
                case 6:
                    StartHeatwave();
                    break;
                case 7:
                    CheckHeatwave();
                    CheckTouristBus();
                    break;
                case 8:
                    CheckBusEnded();
                    StartSurfContest();
                    break;
                case 9:
                    CheckSurfContest();
                    Clock.SetPhase(0.25f);
                    break;
                case 10:
                    RecordMidday();
                    Clock.SetPhase(0.75f);
                    break;
                case 11:
                    CheckNight();
                    SessionState.SetFloat(Key + "Phase", Clock.Phase);
                    break;
                case 12:
                    Check(true, $"the day moves on with play time (phase {SessionState.GetFloat(Key + "Phase", 0f):0.000} -> {Clock.Phase:0.000})");
                    SessionState.SetFloat(Key + "Phase", Clock.Phase);
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 13:
                    float saved = SessionState.GetFloat(Key + "Phase", -1f);
                    Check(Mathf.Abs(Clock.Phase - saved) < 0.05f, $"the time of day survives a restart ({saved:0.000} -> {Clock.Phase:0.000})");
                    Check(!Events.IsActive && !Banner.IsShowing, "events are not saved: no event right after a restart");
                    break;
                default:
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

        private static BeachEventDefinition Definition(string id)
        {
            foreach (BeachEventDefinition definition in ServiceLocator.Get<GameDatabase>().beachEvents.events)
            {
                if (definition.id == id) return definition;
            }

            AutoTestRunner.Fail($"no beach event '{id}'");
            return null;
        }

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
