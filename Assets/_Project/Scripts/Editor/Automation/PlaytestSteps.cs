using System.IO;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 2 playthrough at 8x speed: tent, visitor sale, cabin via the panel, towels, a decoration,
    /// bungalow/hotel/resort with their products, a neighbour moving in, a completed quest, rent, then a
    /// Play-mode restart to check everything is restored. Captures HUD and portrait world screenshots.
    /// </summary>
    internal static class PlaytestSteps
    {
        private const float TestTimeScale = 8f;
        private const long TestGrant = 200000;
        private const string Key = "BeachTycoon.AutoRun.";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static StandService Stands => ServiceLocator.Get<StandService>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static VisitorService Visitors => ServiceLocator.Get<VisitorService>();
        private static NeighbourService Neighbours => ServiceLocator.Get<NeighbourService>();
        private static DecorationService Decorations => ServiceLocator.Get<DecorationService>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return Buildings.GetLot(0).state == LotState.Built && PlaytestTools.Find<StandView>() != null;
                case 2: return PlaytestTools.Find<CoinPickupView>() != null;
                case 4: return Buildings.MainTier == 2;
                case 6: return Buildings.MainTier == 3;
                case 7: return Buildings.MainTier == 4;
                case 8: return Buildings.MainTier == 5 && Visitors.ActiveCount >= 12;
                case 10: return Neighbours.Count > 0 && Buildings.GetLot(Neighbours.Get(0).lotIndex).state == LotState.Built;
                case 11: return Neighbours.Count > 0 && Neighbours.Get(0).questsCompleted > 0;
                case 12: return HasRentCoin();
                case 15: return Visitors.ActiveCount >= 30;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0:
                    Time.timeScale = TestTimeScale;
                    PlaytestTools.CapturePortrait("m2_0_island_portrait.png");
                    Check(Economy.Coins == 0, "new game starts with 0 coins");
                    PlaytestTools.TapWorld(PlaytestTools.Find<LotView>().transform.position);
                    break;
                case 1:
                    AutoTestRunner.Write("Tent built");
                    break;
                case 2:
                    long before = Economy.Coins;
                    PlaytestTools.TapWorld(PlaytestTools.Find<CoinPickupView>().transform.position);
                    Check(Economy.Coins > before, $"visitor sale coin collected ({Economy.Coins})");
                    PlaytestTools.Capture("m2_1_tent.png");
                    break;
                case 3:
                    Economy.Earn(TestGrant);
                    PlaytestTools.TapAndUpgrade(PlaytestTools.Find<BuildingView>().transform.position + Vector3.up);
                    PlaytestTools.Click(PlaytestTools.Find<UpgradePanelView>(), "UpgradeButton");
                    Check(Buildings.GetLot(0).state == LotState.Building, "cabin started from the upgrade panel");
                    break;
                case 4:
                    PlaytestTools.ClickProduct(1);
                    PlaytestTools.Find<DecorPanelView>().Open();
                    PlaytestTools.Click(PlaytestTools.Find<DecorPanelView>(), "DecorButton(Clone)");
                    Check(Decorations.Count == 1, "beach umbrella bought from the decor shop");
                    PlaytestTools.Capture("m2_2_decor_shop.png");
                    PlaytestTools.Find<DecorPanelView>().Close();
                    Check(Buildings.TryUpgrade(0), "bungalow started");
                    break;
                case 6:
                    PlaytestTools.ClickProduct(2);
                    Check(Stands.FindByProduct("drinks") != null, "drinks unlocked at the bungalow");
                    PlaytestTools.CapturePortrait("m2_3_bungalow_portrait.png");
                    Check(Buildings.TryUpgrade(0), "hotel started");
                    break;
                case 7:
                    PlaytestTools.ClickProduct(3);
                    Check(Stands.FindByProduct("surf_lessons") != null, "surf lessons unlocked at the hotel");
                    PlaytestTools.CapturePortrait("m2_4_hotel_portrait.png");
                    Check(Buildings.TryUpgrade(0), "resort started");
                    break;
                case 8:
                    PlaytestTools.ClickProduct(4);
                    Check(Stands.Count == 5, $"all five stands open ({Stands.Count})");
                    AutoTestRunner.Write($"Visitors {Visitors.ActiveCount}, popularity {ServiceLocator.Get<PopularityService>().Value:0.00}");
                    PlaytestTools.CapturePortrait("m2_5_resort_portrait.png");
                    PlaytestTools.Capture("m2_5_resort_hud.png");
                    break;
                case 9:
                    for (int i = 0; i < 200 && !Neighbours.HasPendingRequest; i++)
                    {
                        EventBus.Publish(new VisitorPurchased(0, ServiceLocator.Get<GameDatabase>().neighbours.minPurchasesToAsk));
                    }

                    Check(Neighbours.HasPendingRequest, "a repeat visitor asked to move in");
                    Check(PlaytestTools.Find<NeighbourRequestView>().IsOpen, "move-in card is shown");
                    PlaytestTools.Capture("m2_6_neighbour_request.png");
                    PlaytestTools.Click(PlaytestTools.Find<NeighbourRequestView>(), "Accept");
                    Check(Neighbours.Count == 1, "neighbour accepted and house started");
                    break;
                case 10:
                    Check(ServiceLocator.Get<QuestService>().GetQuest(0) != null, $"neighbour got a quest: {ServiceLocator.Get<QuestService>().Describe(0)}");
                    NeighbourModel house = Neighbours.Get(0);
                    PlaytestTools.FocusCamera(new Vector3(-5.5f, 0f, -9.4f), 24f);
                    PlaytestTools.CapturePortrait("m2_7_houses_portrait.png");
                    AutoTestRunner.Write($"{house.name} lives on lot {house.lotIndex}");
                    break;
                case 11:
                    Check(true, $"quest completed, next: {ServiceLocator.Get<QuestService>().Describe(0)}");
                    break;
                case 12:
                    Check(true, "house dropped a rent coin");
                    SessionState.SetString(Key + "Coins", Economy.Coins.ToString());
                    ServiceLocator.Get<ISaveService>().Save();
                    AutoTestRunner.Write("Save bytes: " + File.ReadAllText(ServiceLocator.Get<ISaveService>().FilePath).Length);
                    AutoTestRunner.RestartPlayMode();
                    break;
                case 13:
                    Time.timeScale = TestTimeScale;
                    Check(Buildings.MainTier == 5 && Stands.Count == 5, "resort and its stands restored");
                    Check(Neighbours.Count == 1 && Decorations.Count == 1, "neighbour and decoration restored");
                    Check(PlaytestTools.Find<DecorationLayer>().transform.childCount == 1, "decoration re-spawned on the beach");
                    PlaytestTools.Find<DailyRewardPanelView>().Close(); // opened by itself after the restart; keep the HUD shots clear
                    break;
                case 15:
                    AutoTestRunner.Write($"Busy beach: {Visitors.ActiveCount} visitors, frame time {Time.smoothDeltaTime / TestTimeScale * 1000f:0.0} ms");
                    PlaytestTools.FocusCamera(new Vector3(0f, 0f, 0f), 34f);
                    PlaytestTools.CapturePortrait("m2_8_busy_portrait.png");
                    PlaytestTools.Capture("m2_8_busy_hud.png");
                    break;
                case 5:
                case 14:
                    break; // waits for the next step's condition
                default:
                    AutoTestRunner.Finish();
                    break;
            }
        }

        private static bool HasRentCoin()
        {
            foreach (CoinPickupView coin in PlaytestTools.Find<CoinSpawner>().GetComponentsInChildren<CoinPickupView>(false))
            {
                if (coin.SourceId >= 1000) return true;
            }

            return false;
        }

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
