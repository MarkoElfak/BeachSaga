using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 4 playtest. Moving: a decoration through the real hold/drag/release events, an invalid drop
    /// that slides back, the main building with its stands (visitors keep buying), positions after a restart.
    /// Performance: resort with five stands and 60 visitors at 1x speed, measuring frame time, draw batches
    /// and managed allocations per frame (FrameAllocationProbe).
    /// </summary>
    internal static class MovePlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const long TestGrant = 5000000;
        private const int StressVisitors = 60;
        private const int WarmupFrames = 120;
        private const int MeasuredFrames = 300;
        private static readonly Vector2 DecorationTarget = new Vector2(-8f, -5f);
        private static readonly Vector2 MainTarget = new Vector2(1f, -2f);

        private static int _salesSinceMove;
        private static FrameAllocationProbe _probe;

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static DecorationService Decorations => ServiceLocator.Get<DecorationService>();
        private static PlacementService Placement => ServiceLocator.Get<PlacementService>();
        private static VisitorService Visitors => ServiceLocator.Get<VisitorService>();
        private static MoveController Mover => PlaytestTools.Find<MoveController>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 5: return _salesSinceMove > 0;
                case 7: return Buildings.MainTier == 2;
                case 8: return Buildings.MainTier == 3;
                case 9: return Buildings.MainTier == 4;
                case 10: return Buildings.MainTier == 5;
                case 11: return _probe != null && _probe.Frames >= WarmupFrames;
                case 12: return _probe != null && _probe.Frames >= MeasuredFrames;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0:
                    Time.timeScale = TestTimeScale;
                    Economy.Earn(TestGrant);
                    Check(Buildings.TryBuild(0), "tent started");
                    break;
                case 1:
                    Check(Decorations.TryBuy(ServiceLocator.Get<GameDatabase>().decorations[0]), "decoration bought");
                    break;
                case 2:
                    MoveDecorationWithGestures();
                    break;
                case 3:
                    DropOnTowelsSlidesBack();
                    break;
                case 4:
                    MoveMainBuilding();
                    break;
                case 5:
                    Check(true, $"visitors keep buying after the move ({_salesSinceMove} sale(s))");
                    PlaytestTools.FocusCamera(new Vector3(MainTarget.x, 0f, MainTarget.y), 30f);
                    PlaytestTools.CapturePortraitWithHud("m4_2_moved_building.png");
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 6:
                    Time.timeScale = TestTimeScale;
                    Check(Placement.GetLotPosition(0) == MainTarget && Near(LotRoot(0).position, MainTarget), "moved building restored after restart");
                    Check(Placement.GetDecorationPosition(0) == DecorationTarget && Near(DecorationRoot(0).position, DecorationTarget), "moved decoration restored after restart");
                    Economy.Earn(TestGrant);
                    Check(Buildings.TryUpgrade(0), "cabin started");
                    break;
                case 7:
                case 8:
                case 9:
                    PlaytestTools.ClickProduct(step - 6);
                    Check(Buildings.TryUpgrade(0), $"tier {step - 4} started");
                    break;
                case 10:
                    PlaytestTools.ClickProduct(4);
                    Check(ServiceLocator.Get<StandService>().Count == 5, "resort with five stands");
                    while (Visitors.ActiveCount < StressVisitors && Visitors.TrySpawn()) { }
                    Time.timeScale = 1f;
                    _probe = FrameAllocationProbe.Create();
                    break;
                case 11:
                    _probe.ResetStats(); // first frames after the burst warm pools and caches
                    break;
                case 12:
                    ReportPerformance();
                    break;
                default:
                    AutoTestRunner.Finish();
                    break;
            }
        }

        private static void MoveDecorationWithGestures()
        {
            Vector2 start = Placement.GetDecorationPosition(0);
            EventBus.Publish(new HoldStarted(Screen(start)));
            Check(Mover.IsMoving, "long-press picks up the decoration");
            EventBus.Publish(new CapturedPointerMoved(Screen(DecorationTarget)));
            Check(Mover.TargetIsValid && Mover.Target == DecorationTarget, $"free spot shows green ({Mover.Target})");
            Check(DecorationRoot(0).position.y > 0.3f, "picked-up decoration floats while dragged");
            PlaytestTools.FocusCamera(new Vector3(DecorationTarget.x, 0f, DecorationTarget.y), 22f);
            PlaytestTools.CapturePortraitWithHud("m4_0_drag_decoration.png");
            EventBus.Publish(new CapturedPointerReleased(Screen(DecorationTarget)));
            Check(!Mover.IsMoving && Decorations.Get(0).placed, "release drops it");
            Check(Placement.GetDecorationPosition(0) == DecorationTarget && Near(DecorationRoot(0).position, DecorationTarget), "decoration moved and saved in its model");
        }

        private static void DropOnTowelsSlidesBack()
        {
            Vector3 before = LotRoot(0).position;
            Vector2 towels = ServiceLocator.Get<GameDatabase>().world.relaxArea.center;
            Check(Mover.TryBegin(Screen(Placement.GetLotPosition(0))), "long-press picks up the main building");
            Mover.DragTo(Screen(towels));
            Check(!Mover.TargetIsValid, "the towel area shows red");
            PlaytestTools.FocusCamera(new Vector3(towels.x, 0f, towels.y), 30f);
            PlaytestTools.CapturePortraitWithHud("m4_1_invalid_drop.png");
            Check(!Mover.Drop(), "dropping on a blocked spot does not move it");
            Check(LotRoot(0).position == before && !Buildings.GetLot(0).placed, "building slid back to where it was");
        }

        private static void MoveMainBuilding()
        {
            Transform stand = PlaytestTools.Find<StandView>().transform;
            float standX = stand.position.x;
            Check(Mover.TryBegin(Screen(Placement.GetLotPosition(0))), "picked up the main building again");
            Mover.DragTo(Screen(MainTarget));
            Check(Mover.TargetIsValid, "a free spot next to it is green");
            Check(Mover.Drop(), "building dropped on the free spot");
            Check(Near(LotRoot(0).position, MainTarget) && Buildings.GetLot(0).placed, "building and save model moved");
            Check(Mathf.Abs(stand.position.x - standX - (MainTarget.x - 0f)) < 0.01f, "its stands moved with it");
            _salesSinceMove = 0;
            EventBus.Subscribe<SaleCompleted>(OnSale);
        }

        private static void ReportPerformance()
        {
            double avgMs = _probe.TotalFrameSeconds / _probe.Frames * 1000.0;
            AutoTestRunner.Write($"Performance at 1x: {Visitors.ActiveCount} visitors, {_probe.Frames} frames, " +
                                 $"avg {avgMs:0.0} ms, worst {_probe.MaxFrameSeconds * 1000f:0.0} ms");
            AutoTestRunner.Write($"Rendering: {UnityStats.drawCalls} draw calls ({UnityStats.staticBatches} static batches, {UnityStats.instancedBatches} instanced), {UnityStats.setPassCalls} SetPass calls, " +
                                 $"{UnityStats.triangles / 1000} k triangles, palms static batched: {PlaytestTools.Find<BeachWorldBuilder>().PalmsStaticBatched}");
            AutoTestRunner.Write($"Managed allocations in game scripts: {_probe.FramesWithAllocations} of {_probe.Frames} frames, " +
                                 $"{_probe.TotalBytes} bytes total, worst frame {_probe.MaxFrameBytes} bytes");
            AutoTestRunner.Write(PlaytestTools.RendererBreakdown());
            Check(Visitors.ActiveCount >= StressVisitors - 5, $"stress test kept ~{StressVisitors} visitors");
            Check(_probe.FramesWithAllocations == 0, "no per-frame allocations with 60 visitors");
            PlaytestTools.CapturePortraitWithHud("m4_3_sixty_visitors.png");
        }

        private static void OnSale(SaleCompleted evt) => _salesSinceMove++;

        private static Vector2 Screen(Vector2 groundXZ)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(new Vector3(groundXZ.x, 0f, groundXZ.y));
            return new Vector2(screen.x, screen.y);
        }

        private static Transform LotRoot(int index) => PlaytestTools.Find<LotView>().transform.parent.Find($"Lot {index}");

        private static Transform DecorationRoot(int index) => PlaytestTools.Find<DecorationLayer>().transform.GetChild(index);

        private static bool Near(Vector3 world, Vector2 xz) => new Vector2(world.x - xz.x, world.z - xz.y).sqrMagnitude < 0.0001f;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
