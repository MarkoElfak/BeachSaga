using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Polish playtest: stand queues form two short lines, every full-screen window fits the Game view with its
    /// X on screen and blocks map input, tapping selects an object and the floating arrows turn it (blocked turns
    /// say so), queues turn with their stand, and turns survive a restart.
    /// </summary>
    internal static partial class PolishPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const float QueueWaitSeconds = 6f;
        private const string Key = "BeachTycoon.AutoRun.Polish.";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static PlacementService Placement => ServiceLocator.Get<PlacementService>();
        private static SelectionService Selection => ServiceLocator.Get<SelectionService>();

        private static float _queueWaitStart;

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return Time.unscaledTime - _queueWaitStart >= QueueWaitSeconds;
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
                    SkipTutorial();
                    Economy.Earn(200000);
                    Check(Buildings.TryBuild(0), "tent started");
                    break;
                case 1:
                    CheckQueueLines();
                    CheckModals();
                    _queueWaitStart = Time.unscaledTime;
                    break;
                case 2:
                    PlaytestTools.Find<BeachTycoon.UI.AchievementPanelView>().Close();
                    Check(!Gestures.IsBlocked, "closing the last window gives the map its input back");
                    PlaytestTools.FocusCamera(PlaytestTools.Find<StandView>().transform.position, 14f);
                    PlaytestTools.Capture("polish_2_queue.png");
                    break;
                case 3:
                    CheckBuildingTurn(); // leaves the building selected for the screenshot
                    break;
                case 4:
                    CheckStandTurn();
                    CheckDecorationTurn();
                    SessionState.SetFloat(Key + "StandYaw", Placement.GetYaw(PlaceableId.Stand(0)));
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 5:
                    CheckAfterRestart();
                    break;
                default:
                    AutoTestRunner.Finish();
                    break;
            }
        }

        private static void SkipTutorial()
        {
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
        }

        private static PointerGestureSource Gestures => PlaytestTools.Find<PointerGestureSource>();

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
