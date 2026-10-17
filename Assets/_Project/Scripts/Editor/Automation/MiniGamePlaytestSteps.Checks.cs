using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The mini-games playtest's checks: opening each game from its stand, rewards, replays, losing, saving.</summary>
    internal static partial class MiniGamePlaytestSteps
    {
        private static readonly string[] ShotNames = { "donut_rush", "towel_run", "perfect_pour", "surf_dodge", "sky_builder" };
        private static bool _saleChecked, _saleOk;
        private static string _saleText;

        private static SelectionService Selection => ServiceLocator.Get<SelectionService>();
        private static PlacementService Placement => ServiceLocator.Get<PlacementService>();

        private static void ResetChecks()
        {
            _saleChecked = _saleOk = false;
        }

        private static bool IsGameStepReady(int phase, string product)
        {
            if (phase == 0) return Panel.ShowsIntro && Panel.Definition.product.id == product;
            if (phase == 1) return Panel.IsPlaying && Panel.Game.Score >= Mathf.Max(1, Panel.Game.Target / 2) && ShowsAction(Panel.Game);
            return Panel.ShowsResult;
        }

        private static void RunGameStep(int phase, int game)
        {
            string product = Products[game];
            if (phase == 0)
            {
                Check(Panel.LevelText == $"Level 1 of {Panel.Definition.LevelCount}", $"{Panel.Definition.title}: the intro shows '{Panel.LevelText}'");
                if (game == 0) PlaytestTools.CapturePortraitWithHud("mg_0_intro.png");
                Click("PlayButton");
                Check(Panel.IsPlaying && Panel.Game.Kind == Panel.Definition.kind && Panel.LevelIndex == 0, $"Play starts {Panel.Game.Kind} level 1");
            }
            else if (phase == 1)
            {
                PlaytestTools.CapturePortraitWithHud($"mg_{game + 1}_{ShotNames[game]}.png");
            }
            else
            {
                CheckFirstWin(product);
                Panel.Close();
                if (game + 1 < Products.Length) OpenFromStand(Products[game + 1]);
                else OpenFromStand(Products[0]);
            }
        }

        // Every stand starts the same: the surf school at the water, and a Play button over each selected stand.
        private static void CheckStandsAndShore()
        {
            UnlockProducts();
            Check(Stands.Count == Products.Length, $"all {Stands.Count} stands are built");
            DailyRewardPanelView daily = PlaytestTools.Find<DailyRewardPanelView>();
            if (daily != null && daily.IsOpen) daily.Close();

            int surf = Stands.FindByProduct("surf_lessons").Index;
            PlaceableId id = PlaceableId.Stand(surf);
            Vector2 at = Placement.GetStandPosition(surf);
            Check(Placement.TouchesShore(Placement.GetFootprint(id, at)), $"the surf school stands at the water ({at.x:0.0}, {at.y:0.0})");
            Vector2 inland = new Vector2(at.x, Placement.UnlockedArea.center.y);
            Check(!Placement.CanPlace(id, inland), "it cannot be moved inland");
            Check(Placement.TryFindFreeSpot(id, inland, out Vector2 spot) && Placement.TouchesShore(Placement.GetFootprint(id, spot)),
                $"a free spot searched from mid-beach is still at the water ({spot.x:0.0}, {spot.y:0.0})");
            PlaytestTools.FocusCamera(new Vector3(at.x, 0f, at.y - 2f), 24f);
            PlaytestTools.CapturePortrait("mg_surf_school_shore.png");
            OpenFromStand(Products[0]);
        }

        // A real tap on the stand selects it; the floating Play button opens its mini-game.
        private static void OpenFromStand(string product)
        {
            StandView view = ViewFor(product);
            PlaytestTools.FocusCamera(view.transform.position, 14f);
            Vector3 point = view.transform.position + Vector3.up * 0.8f;
            PlaytestTools.TapWorld(point);
            bool selected = Selection.HasSelection && Selection.Selected.Equals(view.Placeable);
            Check(selected, selected ? $"tapping the {product} stand selects it"
                : $"tapping the {product} stand selects it (the tap hit {PlaytestTools.HitName(point)}, selection: {Selection.HasSelection})");
            GameObject play = PlaytestTools.Find<SelectionArrowsView>().transform.Find("Bar/PlayButton").gameObject;
            Check(play.activeSelf, "the Play button shows over it");
            PlaytestTools.Click(PlaytestTools.Find<SelectionArrowsView>(), "PlayButton");
            Check(Panel.IsOpen && !Selection.HasSelection, "Play opens the mini-game");
        }

        private static void CheckFirstWin(string product)
        {
            StandController stand = Stands.FindByProduct(product);
            Check(Panel.Game.State == MiniGameState.Won && Panel.LastRunRewarded, $"{Panel.Definition.title} level 1 won ({Panel.Game.Score} of {Panel.Game.Target}, {Panel.Game.Elapsed:0.0} s)");
            Check(Games.LevelsPassed(product) == 1 && Mathf.Approximately(Games.IncomeMultiplier(stand.Product), 1.1f),
                $"{stand.Product.displayName} now sell for +10%");
            Check(ViewFor(product).UpgradesShown == 1, "the stand shows its first decoration");
            if (product == Products[0]) PlaytestTools.CapturePortraitWithHud("mg_6_result.png");
        }

        private static void CheckDonutLevel(int level)
        {
            Check(Panel.Game.State == MiniGameState.Won && Panel.LastRunRewarded && Panel.LevelIndex == level - 1,
                $"donut rush level {level} won ({Panel.Game.Score} of {Panel.Game.Target}, {Panel.Game.Elapsed:0.0} s)");
            Check(Games.LevelsPassed("donuts") == level && ViewFor("donuts").UpgradesShown == level, $"{level} levels passed, {level} decorations");
            if (level == 5) MiniGamePanelView.Autopilot = false; // the replay is played by tapping, like a player
            Click("AgainButton");
        }

        private static void CheckReplay()
        {
            StandController donuts = Stands.FindByProduct("donuts");
            Check(Panel.Game.State == MiniGameState.Won && !Panel.LastRunRewarded && Panel.LevelIndex == 4,
                "replaying level 5 by tapping the icings: won, the result shows, no extra reward");
            Check(Games.IsComplete(Panel.Definition) && Mathf.Approximately(Games.IncomeMultiplier(donuts.Product), 1.5f), "all 5 donut levels: donuts sell for +50%");
            Panel.Close();
            StandView view = ViewFor("donuts");
            PlaytestTools.FocusCamera(view.transform.position, 9f);
            PlaytestTools.CapturePortrait("mg_7_donut_stand_upgraded.png");

            // A run with no input at all must be lost.
            MiniGamePanelView.Autopilot = false;
            OpenFromStand("towels");
            Click("PlayButton");
            Check(Panel.IsPlaying && Panel.LevelIndex == 1, "towel run level 2 started, autopilot off");
        }

        // Clicks the icing button the customer wants, as a player would; ready once the result page shows.
        private static bool TapWantedIcing()
        {
            if (Panel.ShowsResult) return true;
            if (Panel.Game is DonutRushGame donut && Panel.IsPlaying) PlaytestTools.Click(Panel.ActiveView, $"Icing{donut.Wanted}");
            return false;
        }

        private static void CheckLostRun()
        {
            Check(Panel.Game.State == MiniGameState.Lost && !Panel.LastRunRewarded, $"no input loses: '{Panel.ResultTitleText}' ({Panel.Game.Score} of {Panel.Game.Target})");
            Check(Games.LevelsPassed("towels") == 1, "a lost run changes nothing");
            PlaytestTools.CapturePortraitWithHud("mg_8_lost.png");
            Panel.Close();
            MiniGamePanelView.Autopilot = true;
            EventBus.Subscribe<SaleCompleted>(OnSale);
        }

        private static void CheckSaleAndRestart()
        {
            Check(_saleOk, _saleText);
            EventBus.Unsubscribe<SaleCompleted>(OnSale);
            ServiceLocator.Get<ISaveService>().Save();
            AutoTestRunner.RestartPlayMode();
        }

        private static void CheckAfterRestart()
        {
            for (int i = 0; i < Products.Length; i++)
            {
                int expected = i == 0 ? 5 : 1;
                Check(Games.LevelsPassed(Products[i]) == expected && ViewFor(Products[i]).UpgradesShown == expected,
                    $"{Products[i]}: {expected} levels and decorations kept after a restart");
            }

            int surf = Stands.FindByProduct("surf_lessons").Index;
            Check(Placement.TouchesShore(Placement.GetFootprint(PlaceableId.Stand(surf), Placement.GetStandPosition(surf))), "the surf school is still at the water");
            DailyRewardPanelView daily = PlaytestTools.Find<DailyRewardPanelView>();
            if (daily != null && daily.IsOpen) daily.Close();
            PlaytestTools.FocusCamera(new Vector3(-2f, 0f, -4f), 40f);
            PlaytestTools.CapturePortrait("mg_9_beach.png");
        }

        // The next donut sale: price x spend x level x boost x season x the +50% mini-game bonus.
        private static void OnSale(SaleCompleted evt)
        {
            StandController stand = Stands.Get(evt.StandIndex);
            if (_saleChecked || evt.VisitorId < 0 || stand.Product.id != "donuts") return;

            float spend = ServiceLocator.Get<VisitorService>().Get(evt.VisitorId).Type.spendMultiplier;
            float bonus = ServiceLocator.Get<GameDatabase>().economy.standLevelIncomeBonus;
            long plain = EconomyMath.ApplyMultiplier(EconomyMath.SaleValue(stand.Product.price, spend, stand.Model.level, bonus),
                ServiceLocator.Get<IncomeBoost>().Multiplier);
            plain = EconomyMath.ApplyMultiplier(plain, ServiceLocator.Get<SeasonService>().IncomeMultiplier);
            long expected = EconomyMath.ApplyMultiplier(plain, 1.5f);
            expected = EconomyMath.ApplyMultiplier(expected, ServiceLocator.Get<BeachEventService>().PriceMultiplier(stand.Product));
            _saleOk = evt.Amount == expected && evt.Amount > plain;
            _saleText = $"a real donut sale includes the +50% bonus ({plain} -> {evt.Amount} coins)";
            _saleChecked = true;
        }

        private static StandView ViewFor(string product)
        {
            foreach (StandView view in Object.FindObjectsByType<StandView>())
            {
                if (view.Stand != null && view.Stand.Product.id == product) return view;
            }

            AutoTestRunner.Fail($"no {product} stand in the scene");
            return null;
        }
    }
}
