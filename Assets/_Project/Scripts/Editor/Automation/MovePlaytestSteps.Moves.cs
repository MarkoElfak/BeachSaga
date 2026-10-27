using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The individual move scenarios and land checks of the M4 playtest.</summary>
    internal static partial class MovePlaytestSteps
    {
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
            Check(Mathf.Abs(stand.position.x - standX) < 0.01f, "its stand stayed where it was (buildings move on their own)");
            _salesSinceMove = 0;
            EventBus.Subscribe<SaleCompleted>(OnSale);
        }

        private static void MoveStand()
        {
            StandView stand = PlaytestTools.Find<StandView>();
            Check(Mover.TryBegin(Screen(Placement.GetStandPosition(0))), "long-press picks up a stand on its own");
            Mover.DragTo(Screen(StandTarget));
            Check(Mover.TargetIsValid && Mover.Target == StandTarget, $"stand target is green ({Mover.Target})");
            PlaytestTools.FocusCamera(new Vector3(StandTarget.x, 0f, StandTarget.y - 2f), 26f);
            PlaytestTools.CapturePortraitWithHud("m4_1b_drag_stand.png");
            Check(Mover.Drop(), "stand dropped");
            bool queueMoved = ServiceLocator.Get<WorldNavigation>().TryGetQueueSlot(0, 0, out Vector3 slot) && slot.z < StandTarget.y;
            Check(Near(stand.transform.position, StandTarget) && queueMoved, "stand and its queue moved; the building stayed");
            Check(Near(LotRoot(0).position, MainTarget), "main building did not move with the stand");
        }

        private static void CheckLandUnlocked()
        {
            Check(Placement.UnlockedArea == Placement.AreaForTier(2) && Placement.UnlockedArea.width > Placement.AreaForTier(1).width,
                $"cabin unlocked more land ({Placement.UnlockedArea.width:0.0} m wide)");
            Check(CameraRig().MaxDistance > _zoomAtFirstTier, $"camera can zoom out further ({_zoomAtFirstTier:0} -> {CameraRig().MaxDistance:0})");
        }

        private static void CaptureWholeBeach()
        {
            Check(PlaytestTools.Find<LandFence>() != null, "land fence exists");
            PlaytestTools.FocusCamera(new Vector3(0f, 0f, -5f), CameraRig().MaxDistance);
            PlaytestTools.CapturePortraitWithHud("m4_4_whole_beach.png");
        }

        private static TopDownCameraController CameraRig() => PlaytestTools.Find<TopDownCameraController>();
    }
}
