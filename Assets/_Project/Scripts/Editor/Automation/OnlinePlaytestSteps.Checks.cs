using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The individual checks of the M6 playtest.</summary>
    internal static partial class OnlinePlaytestSteps
    {
        private static long _coinsBeforeVisit;
        private static int _homeBuildings;
        private static int _homeStands;
        private static int _homeDecorations;
        private static Vector2 _homeMain;

        private static void FinishTutorial()
        {
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
        }

        private static void BuyDecorations(int count)
        {
            DecorationDefinition umbrella = ServiceLocator.Get<GameDatabase>().decorations[0];
            for (int i = 0; i < count; i++) ServiceLocator.Get<DecorationService>().TryBuy(umbrella);
        }

        private static void RememberHome()
        {
            _homeBuildings = 0;
            for (int i = 0; i < Buildings.LotCount; i++) _homeBuildings += Buildings.GetLot(i).state == LotState.Built ? 1 : 0;
            _homeStands = ServiceLocator.Get<StandService>().Count;
            _homeDecorations = ServiceLocator.Get<DecorationService>().Count;
            _homeMain = ServiceLocator.Get<PlacementService>().GetLotPosition(0);
            AutoTestRunner.Write($"Home: tier {Buildings.MainTier}, {_homeBuildings} building(s), {_homeStands} stand(s), {_homeDecorations} decoration(s)");
        }

        private static void OpenVisitPanelFromHud()
        {
            PlaytestTools.Find<DailyRewardPanelView>().Close(); // opens by itself once the tutorial is over
            Transform button = PlaytestTools.Find<Canvas>().transform.Find("SafeArea/VisitButton");
            Check(button != null && button.gameObject.activeInHierarchy, "Visit button is on the HUD after the tutorial");
            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "VisitButton");
            VisitPanelView panel = PlaytestTools.Find<VisitPanelView>();
            Check(panel.IsOpen && panel.Owners.Count == 4 && panel.Owners[0] == Visits.PlayerId, "visit list shows my island and three friends");
        }

        private static void VisitRow(int row)
        {
            if (row == 0) PlaytestTools.CapturePortraitWithHud("m6_0_visit_list.png");
            _coinsBeforeVisit = Economy.Coins;
            PlaytestTools.Click(PlaytestTools.Find<VisitPanelView>(), $"Row{row}");
            Check(Visits.IsVisiting, $"visit {row} started");
        }

        private static void CheckOwnIsland()
        {
            IslandSnapshot island = Visits.Current;
            IslandSceneRoot root = PlaytestTools.Find<IslandSceneRoot>();
            int bytes = IslandSnapshotJson.ByteCount(IslandSnapshotJson.ToJson(island));
            AutoTestRunner.Write($"Own snapshot: {bytes} bytes, {island.buildings.Count} building(s), {island.stands.Count} stand(s), {island.decorations.Count} decoration(s)");
            Check(Visits.IsOwnIsland && island.tier == 2, "my saved snapshot loads in the Island scene");
            Check(bytes < IslandSnapshotJson.MaxBytes, "snapshot JSON is under 50 KB");
            Check(island.stands.Count == _homeStands && island.decorations.Count == _homeDecorations, "every stand and decoration is in the snapshot");
            Check(root.Layout.Placement.GetLotPosition(0) == _homeMain, "objects keep their positions");
            int props = PlaytestTools.Find<IslandPropSpawner>().SpawnedCount;
            Check(props == _homeBuildings + _homeStands + _homeDecorations, $"island shows {props} objects");
            CheckBonus();
            Check(PlaytestTools.Find<LotView>() == null && PlaytestTools.Find<MoveController>() == null
                && PlaytestTools.Find<BuildBarView>() == null, "no build bar, lot logic or move controller on a visit");
            Check(!Button("LikeButton").activeSelf && !Button("GiftButton").activeSelf, "no like or gift on my own island");
            Check(PlaytestTools.Find<VisitCrowd>().ActiveCount > 0, $"simulated visitors walk around ({PlaytestTools.Find<VisitCrowd>().ActiveCount})");

            long coins = Economy.Coins;
            PlaytestTools.TapWorld(new Vector3(_homeMain.x, 1f, _homeMain.y));
            Vector2 stand = root.Layout.Placement.GetStandPosition(0);
            PlaytestTools.TapWorld(new Vector3(stand.x, 0.8f, stand.y));
            Check(Buildings.MainTier == 2 && PlaytestTools.Find<UpgradePanelView>() == null && Economy.Coins >= coins, "taps on the visited island change nothing");
            Check(Analytics.HasLogged(AnalyticsEvents.IslandVisited), "analytics: island_visited");
            PlaytestTools.CapturePortraitWithHud("m6_1_own_island.png");
        }

        private static void CheckHomeRestored()
        {
            int lots = Object.FindObjectsByType<LotView>().Length;
            int stands = Object.FindObjectsByType<StandView>().Length;
            int decorations = PlaytestTools.Find<DecorationLayer>().transform.childCount;
            Check(lots == Buildings.LotCount && stands == _homeStands && decorations == _homeDecorations,
                $"home beach rebuilt after the visit ({lots} lots, {stands} stands, {decorations} decorations)");
            Check(Buildings.MainTier == 2 && ServiceLocator.Get<PlacementService>().GetLotPosition(0) == _homeMain, "home island unchanged by the visit");
            AutoTestRunner.Write($"Home beach banked {Visits.EarnedWhileAway} coins while away");
        }

        private static void CheckFriendIsland()
        {
            IslandSnapshot island = Visits.Current;
            Check(!Visits.IsOwnIsland && island.ownerName == "Kai" && island.tier == 3, "Kai's island loads (tier 3)");
            Check(Text("Banner/Title") == "Kai's island", "HUD names the owner");
            CheckBonus();
            IslandHudView hud = PlaytestTools.Find<IslandHudView>();
            PlaytestTools.Click(hud, "LikeButton");
            Check(Social.HasLiked(Kai) && Text("LikeButton/Label") == "Liked!" && Analytics.HasLogged(AnalyticsEvents.IslandLiked), "like goes through (island_liked)");
            PlaytestTools.Click(hud, "GiftButton");
            Check(Social.HasSentGift(Kai) && Text("GiftButton/Label") == "Gift sent" && Analytics.HasLogged(AnalyticsEvents.GiftSent), "gift goes through (gift_sent)");
            Check(PlaytestTools.Find<VisitCrowd>().ActiveCount >= 3, $"crowd on Kai's island ({PlaytestTools.Find<VisitCrowd>().ActiveCount})");
            PlaytestTools.CapturePortraitWithHud("m6_2_friend_island.png");
            PlaytestTools.Click(hud, "HomeButton");
        }

        private static void CheckBigIsland()
        {
            IslandSnapshot island = Visits.Current;
            Check(island.tier == 5 && island.stands.Count >= 4 && island.buildings.Count >= 4, $"Marina's resort ({island.stands.Count} stands, {island.buildings.Count} buildings)");
            Check(PlaytestTools.Find<VisitCrowd>().ActiveCount >= 5, $"crowd on Marina's island ({PlaytestTools.Find<VisitCrowd>().ActiveCount})");
            PlaytestTools.CapturePortraitWithHud("m6_3_resort_island.png");
            Vector2 main = PlaytestTools.Find<IslandSceneRoot>().Layout.Placement.GetLotPosition(0);
            PlaytestTools.FocusCamera(new Vector3(main.x, 0f, main.y), 55f);
            PlaytestTools.CapturePortrait("m6_4_resort_wide.png");
            PlaytestTools.Click(PlaytestTools.Find<IslandHudView>(), "HomeButton");
        }

        private static void CheckBonus()
        {
            long bonus = Visits.LastBonus;
            Check(bonus > 0 && Economy.Coins >= _coinsBeforeVisit + bonus && Text("Banner/Bonus").Contains("+"), $"visit bonus paid ({bonus})");
        }

        private static GameObject Button(string name) => PlaytestTools.Find<IslandHudView>().transform.Find(name).gameObject;

        private static string Text(string path)
        {
            Transform found = PlaytestTools.Find<IslandHudView>().transform.Find(path);
            return found != null ? found.GetComponent<TMP_Text>().text : string.Empty;
        }
    }
}
