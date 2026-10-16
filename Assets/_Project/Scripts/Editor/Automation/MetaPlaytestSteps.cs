using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using UnityEditor;
using UnityEngine;
using SettingsService = BeachTycoon.Gameplay.SettingsService;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Milestone 3 playthrough: tutorial from an empty beach to the first donut sale (under 60 s of game time),
    /// tutorial upgrade step, rewarded 2x boost, settings, analytics, then a simulated 20 h absence to check
    /// capped offline earnings, the popup and the ad-doubled claim, then the daily reward calendar that
    /// follows it. Runs at 5x speed.
    /// </summary>
    internal static class MetaPlaytestSteps
    {
        private const float TestTimeScale = 5f;
        private const float FirstSaleLimitSeconds = 60f;
        private const double AbsenceHours = 20.0;
        private const string Key = "BeachTycoon.AutoRun.Meta.";

        private static BuildingManager Buildings => ServiceLocator.Get<BuildingManager>();
        private static IEconomyService Economy => ServiceLocator.Get<IEconomyService>();
        private static TutorialService Tutorial => ServiceLocator.Get<TutorialService>();
        private static IncomeBoost Boost => ServiceLocator.Get<IncomeBoost>();
        private static OfflineEarningsService Offline => ServiceLocator.Get<OfflineEarningsService>();
        private static DailyRewardService Daily => ServiceLocator.Get<DailyRewardService>();
        private static MockAnalyticsService Analytics => (MockAnalyticsService)ServiceLocator.Get<IAnalyticsService>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return PlaytestTools.Find<CoinSpawner>().TryGetWaitingCoin(out _);
                case 4: return Boost.IsActive;
                case 6: return Buildings.MainTier == 2;
                case 8: return !Offline.HasPending;
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0:
                    Time.timeScale = TestTimeScale;
                    SessionState.SetFloat(Key + "Start", Time.time);
                    Check(Economy.Coins == 0 && Tutorial.Step == TutorialService.BuildStep, "new game starts at tutorial step 1 with 0 coins");
                    Check(!PlaytestTools.Find<OfflineEarningsPopupView>().IsOpen, "no offline popup on a brand-new game");
                    PlaytestTools.CapturePortraitWithHud("m3_0_tutorial_build.png");
                    PlaytestTools.TapWorld(PlaytestTools.Find<LotView>().transform.position);
                    Check(Tutorial.Step == TutorialService.SellStep, "building the tent advances the tutorial");
                    break;
                case 1:
                    float firstSale = Time.time - SessionState.GetFloat(Key + "Start", 0f);
                    Check(firstSale < FirstSaleLimitSeconds, $"first donut sold {firstSale:0.0} s of game time after the game started (limit {FirstSaleLimitSeconds} s)");
                    PlaytestTools.CapturePortraitWithHud("m3_1_tutorial_coin.png");
                    PlaytestTools.Find<CoinSpawner>().TryGetWaitingCoin(out Vector3 coin);
                    PlaytestTools.TapWorld(coin);
                    Check(Tutorial.Step == TutorialService.UpgradeStep, "collecting the coin advances the tutorial");
                    break;
                case 2:
                    Economy.Earn(100);
                    PlaytestTools.ClickProduct(0);
                    Check(!Tutorial.IsActive, "upgrading the donut stand finishes the tutorial");
                    PlaytestTools.Click(PlaytestTools.Find<BoostButtonView>(), "BoostButton");
                    break;
                case 3:
                    break; // the mock ad "plays" for a moment
                case 4:
                    Check(Mathf.Approximately(Boost.Multiplier, 2f) && Boost.RemainingSeconds > 590f, $"rewarded ad starts 2x income for 10 min ({Boost.RemainingSeconds:0} s left)");
                    PlaytestTools.CapturePortraitWithHud("m3_2_boost_active.png");
                    SettingsPanelView settings = PlaytestTools.Find<SettingsPanelView>();
                    PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "SettingsButton"); // sits on the HUD, outside the panel
                    PlaytestTools.Click(settings, "SoundButton");
                    Check(settings.IsOpen && !ServiceLocator.Get<SettingsService>().SoundOn, "settings panel turns sound off");
                    PlaytestTools.CapturePortraitWithHud("m3_3_settings.png");
                    PlaytestTools.Click(settings, "CloseButton");
                    Check(ServiceLocator.Get<AudioService>().HooksFired > 0, $"audio hooks fired ({ServiceLocator.Get<AudioService>().HooksFired})");
                    break;
                case 5:
                    Economy.Earn(1000);
                    PlaytestTools.TapWorld(PlaytestTools.Find<BuildingView>().transform.position + Vector3.up);
                    PlaytestTools.Click(PlaytestTools.Find<UpgradePanelView>(), "UpgradeButton");
                    break;
                case 6:
                    PlaytestTools.ClickProduct(1); // towels
                    Check(Analytics.HasLogged(AnalyticsEvents.SessionStart), "analytics: session_start");
                    Check(Analytics.HasLogged(AnalyticsEvents.TierUpgraded), "analytics: tier_upgraded");
                    Check(Analytics.HasLogged(AnalyticsEvents.ProductUnlocked), "analytics: product_unlocked");
                    SessionState.SetString(Key + "Coins", Economy.Coins.ToString());
                    AutoTestRunner.RestartPlayModeAfterAbsence(AbsenceHours);
                    break;
                case 7:
                    Time.timeScale = TestTimeScale;
                    EconomyConfig economy = ServiceLocator.Get<GameDatabase>().economy;
                    long expected = OfflineEarnings.Calculate(Offline.IncomePerSecond, System.TimeSpan.FromHours(AbsenceHours),
                        economy.offlineEfficiency, economy.offlineCapHours, economy.offlineMinSeconds);
                    AutoTestRunner.Write($"Income estimate {Offline.IncomePerSecond:0.00}/s; offline reward {Offline.PendingCoins} for {Offline.PendingSeconds / 3600f:0.0} h");
                    Check(Offline.PendingCoins > 0 && Offline.PendingCoins == expected, $"offline earnings = rate x capped hours x efficiency ({expected})");
                    Check(Mathf.Approximately(Offline.PendingSeconds, economy.offlineCapHours * 3600f), "20 h away is capped at 8 h");
                    Check(PlaytestTools.Find<OfflineEarningsPopupView>().IsOpen, "offline earnings popup is shown");
                    PlaytestTools.CapturePortraitWithHud("m3_4_offline_popup.png");
                    SessionState.SetString(Key + "Expected", Offline.DoubledCoins.ToString());
                    PlaytestTools.Click(PlaytestTools.Find<OfflineEarningsPopupView>(), "DoubleButton");
                    break;
                case 8:
                    long before = long.Parse(SessionState.GetString(Key + "Coins", "0"));
                    long doubled = long.Parse(SessionState.GetString(Key + "Expected", "0"));
                    Check(Economy.Coins == before + doubled, $"ad doubles the offline reward ({before} + {doubled} = {Economy.Coins})");
                    Check(Analytics.HasLogged(AnalyticsEvents.OfflineRewardClaimed), "analytics: offline_reward_claimed");
                    Check(!ServiceLocator.Get<SettingsService>().SoundOn, "sound-off setting survived the restart");
                    Check(!Tutorial.IsActive, "tutorial does not come back after a restart");
                    Check(Boost.IsActive, "boost time left survived the restart");
                    DailyRewardPanelView calendar = PlaytestTools.Find<DailyRewardPanelView>();
                    Check(calendar.IsOpen && Daily.CanClaim && Daily.TodayIndex == 0, "daily reward calendar opens after the offline popup, on day 1");
                    PlaytestTools.CapturePortraitWithHud("m3_5_daily_rewards.png");
                    long beforeDaily = Economy.Coins;
                    long dailyCoins = Daily.TodayCoins;
                    PlaytestTools.Click(calendar, "CollectButton");
                    Check(Economy.Coins == beforeDaily + dailyCoins && !Daily.CanClaim && Daily.Streak == 1, $"daily reward collected once ({dailyCoins}), streak 1");
                    Check(!calendar.IsOpen && Analytics.HasLogged(AnalyticsEvents.DailyRewardClaimed), "calendar closes and daily_reward_claimed is logged");
                    PlaytestTools.CapturePortraitWithHud("m3_6_after_claim.png");
                    break;
                default:
                    AutoTestRunner.Finish();
                    break;
            }
        }

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
