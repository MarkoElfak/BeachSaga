using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 15 playtest (camera and zoom), on a portrait phone-shaped view: a double tap on open ground glides
    /// to the whole map and back to a close-up; pinching stretches past both zoom limits and springs back; dragging
    /// looks a little past the land's edge and no further. Every upgrade lets the camera zoom out further, and full
    /// zoom-out shows all the land at Tent, at Resort, on a landscape screen and on a friend's island, with the
    /// haze beyond the map and shadows still drawn.
    /// </summary>
    internal static partial class CameraPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const float Portrait = 720f / 1280f;
        private const float Landscape = 1280f / 720f;
        private const int TopTier = 5;
        private const int FirstUpgradeStep = 6; // steps 6..9 wait for tiers 2..5

        private static float _previousMax;
        private static float _previousArea;

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static TopDownCameraController Rig => Camera.main != null ? Camera.main.GetComponent<TopDownCameraController>() : null;

        public static bool IsReady(int step)
        {
            if (step >= FirstUpgradeStep && step < FirstUpgradeStep + TopTier - 1)
            {
                return BeachReady() && Buildings.MainTier == step - FirstUpgradeStep + 2 && Buildings.GetLot(0).state == LotState.Built
                    && !Rig.IsFlying; // the camera first flies over the land that opened
            }

            switch (step)
            {
                case 1: return BeachReady() && Buildings.GetLot(0).state == LotState.Built && Fitted(Portrait);
                case 2: case 3: case 10: case 14: return !Rig.IsFlying;
                case 4: return Mathf.Approximately(Rig.Distance, Rig.MaxDistance);
                case 5: return Mathf.Approximately(Rig.Distance, Rig.MinDistance);
                case 11: return Fitted(Landscape);
                case 12: return IslandReady();
                case 13: return IslandReady() && Fitted(Portrait);
                case 15: return BeachReady();
                default: return true;
            }
        }

        public static void Run(int step)
        {
            if (step >= FirstUpgradeStep && step < FirstUpgradeStep + TopTier - 1)
            {
                CheckUpgrade(Buildings.MainTier);
                return;
            }

            switch (step)
            {
                case 0: Begin(); break;
                case 1: DoubleTapToWholeMap(); break;
                case 2: CheckWholeMap("Tent", "m15_1_tent_whole.png"); DoubleTapSea(); break;
                case 3: CheckCloseUpAndStretchOut(); break;
                case 4: CheckSpringBackAndStretchIn(); break;
                case 5: CheckDragEdgeAndUpgrade(); break;
                case 10: CheckWholeMap("Resort", "m15_3_resort_whole.png"); Camera.main.aspect = Landscape; break;
                case 11: CheckLandscape(); break;
                case 12: Camera.main.aspect = Portrait; break;
                case 13: Rig.FlyToWholeMap(); break;
                case 14: CheckWholeMap("Marina's island", "m15_5_marina_whole.png"); ServiceLocator.Get<VisitService>().ReturnHome(); break;
                default: Camera.main.ResetAspect(); AutoTestRunner.Finish(); break;
            }
        }

        private static void Begin()
        {
            Time.timeScale = TestTimeScale;
            Camera.main.aspect = Portrait; // a phone held upright, whatever shape the Game view has
            ServiceLocator.Get<IEconomyService>().Earn(500000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void DoubleTapToWholeMap()
        {
            Check(!Rig.ShowsWholeMap && Rig.Distance < Rig.MaxDistance, $"the game starts close up ({Rig.Distance:0} m, whole map at {Rig.MaxDistance:0} m)");
            _previousMax = Rig.MaxDistance;
            _previousArea = Rig.Area.width * Rig.Area.height;
            DoubleTapSea();
        }

        private static void CheckCloseUpAndStretchOut()
        {
            CameraConfig config = ServiceLocator.Get<GameDatabase>().cameraConfig;
            Check(!Rig.ShowsWholeMap && Mathf.Approximately(Rig.Distance, config.startDistance),
                $"a second double tap glides back to the close-up ({Rig.Distance:0} m)");
            PlaytestTools.CapturePortrait("m15_2_closeup.png");

            for (int i = 0; i < 40; i++) EventBus.Publish(new PinchEvent(0.8f, Vector2.zero));
            float stretch = Rig.Distance - Rig.MaxDistance;
            Check(stretch > 0f && stretch < Rig.MaxDistance * config.zoomGive,
                $"pinching out past the limit stretches {stretch:0.0} m and eases to a stop (at most {Rig.MaxDistance * config.zoomGive:0.0} m)");
        }

        private static void CheckSpringBackAndStretchIn()
        {
            CameraConfig config = ServiceLocator.Get<GameDatabase>().cameraConfig;
            Check(Rig.ShowsWholeMap && Vector2.Distance(Flat(Rig.Focus), Rig.Area.center) < 0.01f,
                "letting go springs back to full zoom-out, centred on the land");
            for (int i = 0; i < 40; i++) EventBus.Publish(new PinchEvent(1.25f, Vector2.zero));
            float stretch = Rig.MinDistance - Rig.Distance;
            Check(stretch > 0f && stretch < Rig.MinDistance * config.zoomGive, $"pinching in past the closest zoom stretches {stretch:0.0} m");
        }

        private static void CheckDragEdgeAndUpgrade()
        {
            Check(Mathf.Approximately(Rig.Distance, Rig.MinDistance), "and springs back to the closest zoom, which is unchanged");
            float edgeLook = ServiceLocator.Get<GameDatabase>().cameraConfig.edgeLook;
            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            for (int i = 0; i < 60; i++) EventBus.Publish(new DragEvent(centre, centre + new Vector2(150f, 0f)));
            float past = Rig.Area.xMin - Rig.Focus.x;
            Check(Mathf.Abs(past - edgeLook) < 0.05f, $"dragging stops {past:0.0} m past the land's edge (look {edgeLook:0.0} m)");
            PlaytestTools.CapturePortrait("m15_2_edge.png");
            Check(Buildings.TryUpgrade(0), "cabin started");
        }

        private static void CheckUpgrade(int tier)
        {
            // A strip across the back adds depth, not width, so a width-bound portrait view need not zoom out further.
            Check(Rig.MaxDistance > _previousMax - 0.01f && Rig.Area.width * Rig.Area.height > _previousArea + 1f,
                $"tier {tier} opens land ({_previousArea:0} -> {Rig.Area.width * Rig.Area.height:0} m2) and full zoom-out never shrinks ({_previousMax:0} -> {Rig.MaxDistance:0} m)");
            _previousArea = Rig.Area.width * Rig.Area.height;
            _previousMax = Rig.MaxDistance;
            if (tier < TopTier) Check(Buildings.TryUpgrade(0), $"tier {tier + 1} started");
            else DoubleTapSea();
        }

        private static void CheckLandscape()
        {
            Check(Rig.MaxDistance < _previousMax, $"a landscape screen needs less zoom-out ({_previousMax:0} -> {Rig.MaxDistance:0} m)");
            CheckWholeMap("Resort, landscape", "m15_4_landscape.png", 1280, 720);
            Camera.main.aspect = Portrait;
            ServiceLocator.Get<VisitService>().StartVisit("demo-marina");
        }

        private static bool BeachReady() =>
            SceneManager.GetActiveScene().name == SceneNames.Beach && Rig != null && PlaytestTools.Find<BuildBarView>() != null;

        private static bool IslandReady() =>
            SceneManager.GetActiveScene().name == SceneNames.Island && Rig != null && PlaytestTools.Find<IslandPropSpawner>() != null
            && PlaytestTools.Find<IslandPropSpawner>().SpawnedCount > 0;

        private static bool Fitted(float aspect) => Rig != null && Mathf.Approximately(Rig.MaxDistance, Rig.FitDistanceFor(aspect))
            && Mathf.Approximately(Camera.main.aspect, aspect);

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
