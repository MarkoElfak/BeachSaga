using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>The individual checks of the polish playtest.</summary>
    internal static partial class PolishPlaytestSteps
    {
        private const int QueueSlots = 5;
        private static readonly Vector3[] Slots = new Vector3[QueueSlots];
        private static int _blocked;

        private static void CheckQueueLines()
        {
            VisitorConfig config = ServiceLocator.Get<GameDatabase>().visitors;
            ReadSlots(0);
            float line = Vector3.Distance(Slots[0], Slots[2]);
            float side = Vector3.Distance(Slots[1], Slots[4]);
            Check(Mathf.Abs(line - 2f * config.queueSpacing) < 0.01f, $"first queue line holds 3 visitors ({line:0.00} m from first to third)");
            Check(Mathf.Abs(side - config.queueLineSpacing) < 0.01f && Vector3.Distance(Slots[2], Slots[3]) < side + 0.01f,
                $"4th and 5th visitors wait in a second line beside it ({side:0.00} m apart)");

            Vector2 at = Placement.GetStandPosition(0);
            Rect area = Placement.GetBlockingArea(PlaceableId.Stand(0), at);
            Check(SlotsInside(area), $"the kept-free area {area.width:0.0} x {area.height:0.0} m covers both lines");
        }

        private static void ReadSlots(int standIndex)
        {
            var navigation = ServiceLocator.Get<WorldNavigation>();
            for (int i = 0; i < QueueSlots; i++) navigation.TryGetQueueSlot(standIndex, i, out Slots[i]);
        }

        private static bool SlotsInside(Rect area)
        {
            for (int i = 0; i < QueueSlots; i++)
            {
                if (!area.Contains(new Vector2(Slots[i].x, Slots[i].z))) return false;
            }

            return true;
        }

        // Every full-screen window: fits the Game view with its X on screen, and blocks map input while open.
        private static void CheckModals()
        {
            int count = 0;
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (ModalWindow modal in root.GetComponentsInChildren<ModalWindow>(true))
                {
                    count++;
                    modal.gameObject.SetActive(true);
                    Canvas.ForceUpdateCanvases();
                    Transform window = modal.transform.Find("Window");
                    Transform close = window != null ? window.Find("CloseButton") : null;
                    bool closeShown = close != null && close.gameObject.activeSelf;
                    Check(OnScreen(window) && (!closeShown || OnScreen(close)) && Gestures.IsBlocked,
                        $"{modal.transform.parent.name} fits {Screen.width}x{Screen.height} (scale {window.localScale.x:0.00}), X on screen, map blocked");
                    modal.gameObject.SetActive(false);
                }
            }

            Check(count >= 7 && !Gestures.IsBlocked, $"{count} windows checked, map input back after closing them");
            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "TrophyButton");
            Check(Gestures.IsBlocked, "the map ignores input while Trophies is open");
            PlaytestTools.Capture("polish_1_trophies_fit.png");
        }

        private static bool OnScreen(Transform target)
        {
            var corners = new Vector3[4];
            ((RectTransform)target).GetWorldCorners(corners); // overlay canvas: world corners are screen pixels
            var screen = new Rect(-1f, -1f, Screen.width + 2f, Screen.height + 2f);
            return screen.Contains(corners[0]) && screen.Contains(corners[2]);
        }

        private static void CheckBuildingTurn()
        {
            PlaceableId main = PlaceableId.Lot(BuildingManager.MainLotIndex);
            PlaytestTools.FocusCamera(PlaytestTools.Find<BuildingView>().transform.position, 18f);
            PlaytestTools.TapWorld(PlaytestTools.Find<BuildingView>().transform.position + Vector3.up);
            Check(Selection.HasSelection && Selection.Selected.Equals(main) && Selection.UpgradeLot == 0, "tapping the building selects it");
            Check(Bar().activeSelf, "rotate arrows appear above it");

            bool fits = Placement.CanRotate(main, 1);
            int blockedBefore = _blocked;
            EventBus.Subscribe<RotateBlocked>(OnBlocked);
            ClickArrow("RotateRight");
            EventBus.Unsubscribe<RotateBlocked>(OnBlocked);
            float yaw = Placement.GetYaw(main);
            float shown = PlaytestTools.Find<BuildingView>().transform.eulerAngles.y;
            if (fits) Check(Mathf.Approximately(yaw, 45f) && Mathf.Abs(Mathf.DeltaAngle(shown, 45f)) < 0.5f, $"right arrow turns it 45 degrees (view {shown:0})");
            else Check(yaw == 0f && _blocked == blockedBefore + 1, "no room to turn: it stays and the player is told");
            PlaytestTools.Capture("polish_3_arrows.png");
        }

        // Two left turns; a step where the turned stand and its queue do not fit is skipped.
        private static void CheckStandTurn()
        {
            StandView stand = PlaytestTools.Find<StandView>();
            PlaytestTools.FocusCamera(stand.transform.position, 14f);
            PlaytestTools.TapWorld(stand.transform.position + Vector3.up * 0.8f);
            PlaceableId id = PlaceableId.Stand(stand.Stand.Index);
            Check(Selection.HasSelection && Selection.Selected.Equals(id) && Selection.UpgradeLot == 0, "tapping a stand selects it, with Upgrade");

            ReadSlots(id.Index);
            Vector3 lineBefore = Slots[2] - Slots[0];
            float yawBefore = Placement.GetYaw(id);
            float expected = yawBefore;
            for (int i = 0; i < 2; i++)
            {
                if (Placement.TryGetNextYaw(id, -1, out float next)) expected = next;
                ClickArrow("RotateLeft");
            }

            float yaw = Placement.GetYaw(id);
            Check(Mathf.Approximately(Mathf.DeltaAngle(yaw, expected), 0f) && Mathf.Abs(Mathf.DeltaAngle(stand.transform.eulerAngles.y, yaw)) < 0.5f
                && !Mathf.Approximately(yaw, yawBefore), $"left arrow turns the stand to {yaw:0} degrees (view {stand.transform.eulerAngles.y:0})");
            ReadSlots(id.Index);
            float turned = Vector3.SignedAngle(lineBefore, Slots[2] - Slots[0], Vector3.up);
            Check(Mathf.Abs(Mathf.DeltaAngle(turned, Mathf.DeltaAngle(yawBefore, yaw))) < 1f, $"its queue turns with it ({turned:0} degrees)");
            Check(SlotsInside(Placement.GetBlockingArea(id, Placement.GetPosition(id))), "the kept-free area still covers the turned queue");
        }

        private static void CheckDecorationTurn()
        {
            var decorations = ServiceLocator.Get<DecorationService>();
            Check(decorations.TryBuy(ServiceLocator.Get<GameDatabase>().decorations[0]), "a decoration bought");
            int index = decorations.Count - 1;
            DecorationView view = FindDecoration(index);
            if (view == null)
            {
                AutoTestRunner.Fail("no view for the new decoration");
                return;
            }

            PlaytestTools.FocusCamera(view.transform.position, 14f);
            PlaytestTools.TapWorld(view.GetComponent<Collider>().bounds.center);
            Check(Selection.HasSelection && Selection.Selected.Equals(PlaceableId.Decoration(index)) && Selection.UpgradeLot < 0,
                "tapping a decoration selects it (no Upgrade button)");
            PlaceableId id = PlaceableId.Decoration(index);
            float expected = 0f;
            for (int i = 0; i < 2; i++)
            {
                if (Placement.TryGetNextYaw(id, 1, out float next)) expected = next;
                ClickArrow("RotateRight");
            }

            float yaw = Placement.GetYaw(id), shown = view.transform.eulerAngles.y;
            Check(yaw > 0f && Mathf.Approximately(yaw, expected) && Mathf.Abs(Mathf.DeltaAngle(shown, yaw)) < 0.5f,
                $"two right turns face it {yaw:0} degrees (expected {expected:0}, view {shown:0})");
            SessionState.SetInt(Key + "Decoration", index);
            SessionState.SetFloat(Key + "DecorationYaw", yaw);
            EventBus.Publish(new GroundTapped());
            Check(!Selection.HasSelection && !Bar().activeSelf, "tapping open ground clears the selection and hides the arrows");
        }

        private static void CheckAfterRestart()
        {
            int index = SessionState.GetInt(Key + "Decoration", 0);
            DecorationView view = FindDecoration(index);
            float yaw = SessionState.GetFloat(Key + "DecorationYaw", 0f);
            Check(view != null && yaw > 0f && Mathf.Abs(Mathf.DeltaAngle(view.transform.eulerAngles.y, yaw)) < 0.5f,
                $"the decoration is still turned after a restart ({yaw:0} degrees)");
            float standYaw = SessionState.GetFloat(Key + "StandYaw", 0f);
            float shown = PlaytestTools.Find<StandView>().transform.eulerAngles.y;
            Check(Mathf.Abs(Mathf.DeltaAngle(shown, standYaw)) < 0.5f, $"the stand keeps its turn ({standYaw:0} degrees)");
        }

        private static DecorationView FindDecoration(int index)
        {
            foreach (DecorationView view in Object.FindObjectsByType<DecorationView>())
            {
                if (view.Placeable.Index == index) return view;
            }

            return null;
        }

        private static GameObject Bar() => PlaytestTools.Find<SelectionArrowsView>().transform.Find("Bar").gameObject;
        private static void ClickArrow(string name) => PlaytestTools.Click(PlaytestTools.Find<SelectionArrowsView>(), name);
        private static void OnBlocked(RotateBlocked evt) => _blocked++;
    }
}
