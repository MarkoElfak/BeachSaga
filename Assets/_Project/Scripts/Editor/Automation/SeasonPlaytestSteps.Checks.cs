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
    /// <summary>The individual checks of the M10 playtest.</summary>
    internal static partial class SeasonPlaytestSteps
    {
        private static ISaveService _oldSave;
        private static bool _saleChecked, _saleOk, _rentChecked, _rentOk;
        private static string _saleText, _rentText;

        private static void ResetChecks()
        {
            _oldSave = null;
            _saleChecked = _saleOk = _rentChecked = _rentOk = false;
        }

        // The reboot swaps in fresh services, so a new save service means the new season is running.
        private static bool Rebooted() => _oldSave != null && GameBootstrap.IsReady
            && SceneManager.GetActiveScene().name == SceneNames.Beach
            && ServiceLocator.TryGet(out ISaveService save) && !ReferenceEquals(save, _oldSave)
            && PlaytestTools.Find<SeasonPanelView>() != null;

        private static void OpenPanel()
        {
            Check(Panel.IsButtonShown && Panel.ButtonText == "New season", $"the Resort shows the '{Panel.ButtonText}' button");
            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "SeasonButton");
            long shells = Season.SeashellsToEarn;
            Check(Panel.IsOpen && Panel.CanStart, "the button opens the New season panel, ready to start");
            EconomyConfig economy = ServiceLocator.Get<GameDatabase>().economy;
            Check(shells == SeasonMath.Seashells(Season.SeasonEarned, economy.seashellCoinUnit, economy.seashellExponent) && shells >= 8,
                $"{Season.SeasonEarned} coins earned this season pay {shells} seashells");
            Check(Panel.SummaryText.Contains(shells + " seashells"), "the panel shows the seashells to earn");
            SessionState.SetInt(Key + "Shells", (int)shells);
            SessionState.SetInt(Key + "Decor", Decorations.Count);
            SessionState.SetInt(Key + "Neighbours", Neighbours.Count);
            PlaytestTools.Capture("m10_1_season_panel.png");
        }

        private static void StartNewSeason()
        {
            _oldSave = ServiceLocator.Get<ISaveService>();
            var analytics = (MockAnalyticsService)ServiceLocator.Get<IAnalyticsService>();
            PlaytestTools.Click(Panel, "StartButton");
            Check(!GameBootstrap.IsReady, "Start new season reboots the game");
            Check(analytics.HasLogged(AnalyticsEvents.SeasonStarted), "analytics: season_started");
        }

        private static void CheckReset()
        {
            long shells = SessionState.GetInt(Key + "Shells", -1);
            GameSaveData data = ServiceLocator.Get<ISaveService>().Data;
            Check(Season.Season == 2 && Season.Seashells == shells, $"season 2 starts with {Season.Seashells} seashells");
            Check(Mathf.Approximately(Season.IncomeMultiplier, Expected(shells)), $"all income x{Season.IncomeMultiplier:0.0#}");
            Check(Economy.Coins - data.wallet.seasonEarned == ServiceLocator.Get<GameDatabase>().economy.startingCoins,
                $"coins start over ({Economy.Coins} now, all earned since the reboot)");
            LotModel main = Buildings.GetLot(0);
            Check(main.state == LotState.Empty && main.tier == 0 && Stands.Count == 0, "the main lot is empty again and there are no stands");
            Check(Decorations.Count == SessionState.GetInt(Key + "Decor", -1), $"{Decorations.Count} decorations kept");
            Check(Neighbours.Count == SessionState.GetInt(Key + "Neighbours", -1)
                && Buildings.GetLot(Neighbours.Get(0).lotIndex).state == LotState.Built, $"{Neighbours.Count} neighbour and their house kept");
            Check(Panel.IsButtonShown && Panel.ButtonText == "Season 2", $"the HUD button now reads '{Panel.ButtonText}'");
            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "SeasonButton");
            Check(!Panel.CanStart, "a new season cannot start again before the Resort");
            PlaytestTools.CapturePortraitWithHud("m10_2_new_season_panel.png");
            Panel.Close();
            DailyRewardPanelView daily = PlaytestTools.Find<DailyRewardPanelView>();
            if (daily != null && daily.IsOpen) daily.Close(); // a reboot is a new session, so today's reward may pop up
            PlaytestTools.FocusCamera(new Vector3(-2f, 0f, -4f), 40f);
            PlaytestTools.CapturePortrait("m10_3_island_kept.png");
        }

        private static void WatchIncome()
        {
            EventBus.Subscribe<SaleCompleted>(OnSale);
            EventBus.Subscribe<RentDue>(OnRent);
        }

        private static void CheckIncome()
        {
            Check(_saleOk, _saleText);
            Check(_rentOk, _rentText);
            EventBus.Unsubscribe<SaleCompleted>(OnSale);
            EventBus.Unsubscribe<RentDue>(OnRent);
        }

        private static void CheckAfterRestart()
        {
            long shells = SessionState.GetInt(Key + "Shells", -1);
            Check(Season.Season == 2 && Season.Seashells == shells, "season and seashells survive a restart");
            Check(Mathf.Approximately(Season.IncomeMultiplier, Expected(shells)), "the income bonus survives a restart");
        }

        private static float Expected(long shells) => 1f + shells * ServiceLocator.Get<GameDatabase>().economy.incomeBonusPerSeashell;

        private static void OnSale(SaleCompleted evt)
        {
            StandController stand = Stands.Get(evt.StandIndex);
            if (_saleChecked || evt.VisitorId < 0) return;

            float spend = ServiceLocator.Get<VisitorService>().Get(evt.VisitorId).Type.spendMultiplier;
            float bonus = ServiceLocator.Get<GameDatabase>().economy.standLevelIncomeBonus;
            long plain = EconomyMath.ApplyMultiplier(EconomyMath.SaleValue(stand.Product.price, spend, stand.Model.level, bonus),
                ServiceLocator.Get<IncomeBoost>().Multiplier);
            long expected = EconomyMath.ApplyMultiplier(plain, Season.IncomeMultiplier);
            expected = EconomyMath.ApplyMultiplier(expected, ServiceLocator.Get<BeachEventService>().PriceMultiplier(stand.Product));
            _saleOk = evt.Amount == expected && evt.Amount > plain;
            _saleText = $"a {stand.Product.displayName} sale includes the seashell bonus ({plain} -> {evt.Amount} coins)";
            _saleChecked = true;
        }

        private static void OnRent(RentDue evt)
        {
            if (_rentChecked) return;
            NeighbourConfig config = ServiceLocator.Get<GameDatabase>().neighbours;
            long plain = (long)System.Math.Round(config.rentPerMinute * config.rentIntervalSeconds / 60.0);
            _rentOk = evt.Amount == EconomyMath.ApplyMultiplier(plain, Season.IncomeMultiplier) && evt.Amount > plain;
            _rentText = $"rent includes the seashell bonus ({plain} -> {evt.Amount} coins)";
            _rentChecked = true;
        }
    }
}
