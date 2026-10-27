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
    /// Milestone 16 playtest (bigger map under fog), on a portrait phone-shaped view: at Tent the whole first beach
    /// is open and the four later pieces wait under fog with "Opens at ..." signs; tapping the fog says what opens
    /// it. Each upgrade rolls one piece's fog away, flies the camera over it and names it in a toast; new land takes
    /// buildings, and the islet gets its boardwalk, towels and visitors.
    /// </summary>
    internal static partial class MapPlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const float Portrait = 720f / 1280f;
        private const float Landscape = 1280f / 720f;
        private const int TopTier = 5;
        private const int FirstUpgradeStep = 3; // steps 3..6 wait for tiers 2..5

        private static readonly string[] Shots = { "", "", "m16_3_cabin_open.png", "m16_4_bungalow_open.png", "m16_5_hotel_open.png", "m16_6_resort_open.png" };

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static PlacementService Placement => ServiceLocator.Get<PlacementService>();
        private static TopDownCameraController Rig => Camera.main != null ? Camera.main.GetComponent<TopDownCameraController>() : null;
        private static LandFog Fog => PlaytestTools.Find<BeachWorldBuilder>().Fog;
        private static MapConfig Map => ServiceLocator.Get<GameDatabase>().map;
        private static string Toast => PlaytestTools.Find<ToastView>().Text;

        public static bool IsReady(int step)
        {
            if (step >= FirstUpgradeStep && step < FirstUpgradeStep + TopTier - 1)
            {
                int tier = step - FirstUpgradeStep + 2;
                return BeachReady() && Buildings.MainTier == tier && Buildings.GetLot(0).state == LotState.Built
                    && !Rig.IsFlying && Toast == $"{Map.pieces[PieceFor(tier)].displayName} is open!";
            }

            switch (step)
            {
                case 1: return BeachReady() && Buildings.GetLot(0).state == LotState.Built && !Rig.IsFlying;
                case 2: return Toast.StartsWith(Map.pieces[1].displayName);
                case 7: return !Rig.IsFlying;
                case 8: return VisitorOnIslet() != null;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            if (step >= FirstUpgradeStep && step < FirstUpgradeStep + TopTier - 1)
            {
                CheckOpened(Buildings.MainTier);
                return;
            }

            switch (step)
            {
                case 0: Begin(); break;
                case 1: CheckTentFog(); break;
                case 2: CheckTapOnFog(); break;
                case 7: CheckIsletPaths(); break;
                case 8: CheckVisitorOnIslet(); break;
                default: Camera.main.ResetAspect(); AutoTestRunner.Finish(); break;
            }
        }

        private static void Begin()
        {
            Time.timeScale = TestTimeScale;
            Camera.main.aspect = Portrait;
            ServiceLocator.Get<IEconomyService>().Earn(500000);
            Check(Buildings.TryBuild(0), "tent started");
        }

        private static void CheckTentFog()
        {
            Check(Placement.UnlockedArea == Map.pieces[0].area, $"at Tent the whole first beach can be built on ({Placement.UnlockedArea})");
            Check(Fog.Count == Map.pieces.Length - 1, $"{Fog.Count} locked pieces under fog");
            LandSignView[] signs = Object.FindObjectsByType<LandSignView>();
            Check(signs.Length == Fog.Count && System.Array.Exists(signs, s => s.Text == "West Cove\nOpens at Cabin"),
                $"{signs.Length} signs, one says 'West Cove / Opens at Cabin'");
            Check(!Placement.CanPlace(PlaceableId.Lot(0), new Vector2(-24f, -4f)), "nothing can be built under the fog");
            int islet = PieceFor(TopTier);
            Check(!Placement.IsPieceOpen(islet) && !PlaytestTools.Find<BeachWorldBuilder>().IsBoardwalkShown(islet), "the islet's boardwalk is not built yet");

            Camera.main.aspect = Landscape; // the side pieces only fit a wide view
            PlaytestTools.FocusCamera(new Vector3(0f, 0f, 2f), 66f);
            PlaytestTools.CapturePortrait("m16_1_tent_fog.png", 1280, 720);
            Camera.main.aspect = Portrait;
            PlaytestTools.FocusCamera(new Vector3(-22f, 0f, -3f), 20f);
            PlaytestTools.CapturePortrait("m16_2_sign.png");
            Vector3 cove = new Vector3(-24f, 0f, -3f);
            PlaytestTools.TapWorld(cove);
            Check(PlaytestTools.HitName(cove).Contains("Fog west_cove"), $"a tap on the fog hits it ({PlaytestTools.HitName(cove)})");
        }

        private static void CheckTapOnFog()
        {
            Check(Toast == "West Cove opens at Cabin. Keep upgrading!", $"tapping the fog says what opens it ('{Toast}')");
            Check(Buildings.TryUpgrade(0), "cabin started");
        }

        private static void CheckOpened(int tier)
        {
            int piece = PieceFor(tier);
            LandPiece land = Map.pieces[piece];
            Check(Placement.IsPieceOpen(piece), $"{land.displayName} opened at tier {tier}");
            Check(Fog.Find(piece) == null || !Fog.Find(piece).gameObject.activeSelf, $"{land.displayName}: its fog rolled away");
            Check(Rig.Area.Contains(land.area.center), $"{land.displayName}: the camera can roam over it");
            PlaceableId building = LotIn(land.area, out Vector2 inside);
            Check(Placement.CanPlace(building, inside), $"{land.displayName}: {building} fits on the new land at {inside}");
            PlaytestTools.CapturePortraitWithHud(Shots[tier]);

            if (tier < TopTier)
            {
                Check(Buildings.TryUpgrade(0), $"tier {tier + 1} started");
                Check(!Placement.IsPieceOpen(PieceFor(tier + 1)), "the next piece waits for the upgrade to finish");
            }
            else
            {
                CheckIslet(piece);
            }
        }

        private static void CheckIslet(int piece)
        {
            Check(PlaytestTools.Find<BeachWorldBuilder>().IsBoardwalkShown(piece), "the islet's boardwalk is built");
            PlaytestTools.FocusCamera(new Vector3(0f, 0f, 14f), 34f);
            PlaytestTools.CapturePortrait("m16_7_islet.png");
            Rig.FlyToWholeMap();
        }

        // A neighbour lot whose default spot is on this land, else the main building in its middle (the islet).
        private static PlaceableId LotIn(Rect area, out Vector2 at)
        {
            for (int i = 1; i < Buildings.LotCount; i++)
            {
                at = Placement.GetLotPosition(i);
                if (area.Contains(at)) return PlaceableId.Lot(i);
            }

            at = new Vector2(area.center.x, area.center.y - 1.5f);
            return PlaceableId.Lot(0);
        }

        private static int PieceFor(int tier)
        {
            for (int i = 0; i < Map.pieces.Length; i++)
            {
                if (Map.pieces[i].unlockTier == tier) return i;
            }

            return -1;
        }

        private static bool BeachReady() =>
            SceneManager.GetActiveScene().name == SceneNames.Beach && Rig != null && PlaytestTools.Find<BuildBarView>() != null;

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
