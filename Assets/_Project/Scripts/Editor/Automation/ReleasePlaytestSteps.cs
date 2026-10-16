using System.IO;
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
    /// Milestone 12 playtest (release readiness): a new game asks the privacy question and cannot skip it;
    /// "Only necessary" turns data off. Settings shows the Remove ads price, reopens the privacy window with the
    /// current choice, and Accept turns it on. A rewarded ad still plays a (mock) video until Remove ads is
    /// bought; after that rewards are instant, the buttons stop saying "ad", and Restore finds the purchase.
    /// Finally the save is damaged on disk and the next start restores the backup and says so.
    /// </summary>
    internal static class ReleasePlaytestSteps
    {
        private static int _privacyEvents;
        private static bool _lastAnalytics;

        private static PrivacyService Privacy => ServiceLocator.Get<PrivacyService>();
        private static IAdsService Ads => ServiceLocator.Get<IAdsService>();
        private static IIapService Iap => ServiceLocator.Get<IIapService>();
        private static IncomeBoost Boost => ServiceLocator.Get<IncomeBoost>();
        private static ISaveService Save => ServiceLocator.Get<ISaveService>();
        private static string RemoveAdsId => ServiceLocator.Get<GameDatabase>().monetization.removeAdsProductId;
        private static PrivacyPanelView PrivacyPanel => PlaytestTools.Find<PrivacyPanelView>();
        private static SettingsPanelView SettingsPanel => PlaytestTools.Find<SettingsPanelView>();
        private static ToastView Toast => PlaytestTools.Find<ToastView>();

        public static bool IsReady(int step)
        {
            switch (step)
            {
                case 1: return !PrivacyPanel.IsOpen;
                case 2: return Boost.IsActive;
                case 3: return SceneManager.GetActiveScene().name == SceneNames.Beach && Toast != null && Toast.Text.Length > 0; // it may already have faded
                default: return true;
            }
        }

        public static void Run(int step)
        {
            switch (step)
            {
                case 0: AskOnFirstLaunch(); break;
                case 1: CheckDeclinedThenSettings(); break;
                case 2: CheckRemoveAds(); break;
                case 3: CheckRecoveredSave(); break;
                default: Done(); break;
            }
        }

        private static void AskOnFirstLaunch()
        {
            EventBus.Subscribe<PrivacyChanged>(OnPrivacyChanged);
            PrivacyPanelView panel = PrivacyPanel;
            Check(Privacy.Choice == PrivacyChoice.NotAsked && panel.IsOpen, "a new game opens the privacy question");
            Check(!panel.CanClose, "it has no close button: it must be answered");
            Check(panel.StatusText == "You can change this any time in Settings.", $"it says '{panel.StatusText}'");
            Transform policy = panel.transform.Find("Root/Window/PolicyButton");
            Check(policy != null && !policy.gameObject.activeSelf, "the policy link stays hidden until a privacy policy URL is set");
            PlaytestTools.CapturePortraitWithHud("m12_1_privacy.png");
            PlaytestTools.Click(panel, "NecessaryButton");
        }

        private static void CheckDeclinedThenSettings()
        {
            Check(Privacy.Choice == PrivacyChoice.Declined && _privacyEvents == 1 && !_lastAnalytics,
                "'Only necessary' closes it with analytics and personalised ads off");

            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "SettingsButton");
            SettingsPanelView settings = SettingsPanel;
            string price = Iap.PriceText(RemoveAdsId);
            Check(settings.RemoveAdsText == "Remove ads  " + price, $"Settings shows '{settings.RemoveAdsText}' with the store price");
            Check(settings.RestoreText == "Restore purchases", "and a Restore purchases button");
            PlaytestTools.CapturePortraitWithHud("m12_2_settings.png");

            PlaytestTools.Click(settings, "PrivacyButton");
            PrivacyPanelView panel = PrivacyPanel;
            Check(panel.IsOpen && panel.CanClose && panel.StatusText.Contains("only what the game needs"),
                $"Privacy in Settings reopens it, closable, with the current choice: '{panel.StatusText}'");
            PlaytestTools.CapturePortraitWithHud("m12_3_privacy_from_settings.png");
            PlaytestTools.Click(panel, "AcceptButton");
            Check(Privacy.Choice == PrivacyChoice.Accepted && _privacyEvents == 2 && _lastAnalytics && !panel.IsOpen && settings.IsOpen,
                "Accept turns analytics on and goes back to Settings");

            Check(!AdWording.AdsRemoved(Ads) && AdWording.Watch(Ads) == "Watch ad", "before buying, reward buttons say 'Watch ad'");
            PlaytestTools.Click(PlaytestTools.Find<BoostButtonView>(), "BoostButton");
            Check(!Boost.IsActive, "the 2x boost waits for the (mock) video to finish");
        }

        private static void CheckRemoveAds()
        {
            SettingsPanelView settings = SettingsPanel;
            PlaytestTools.Click(settings, "RemoveAdsButton");
            Check(Iap.IsOwned(RemoveAdsId) && settings.RemoveAdsText == "Ads removed - thank you!", $"buying Remove ads reads '{settings.RemoveAdsText}'");

            bool granted = false;
            Ads.ShowRewarded(AdPlacements.DailyDouble, watched => granted = watched);
            Check(granted, "a rewarded reward is now granted at once, with no video");
            Check(AdWording.Watch(Ads) == "Free", "and reward buttons say 'Free' instead of 'Watch ad'");

            PlaytestTools.Click(settings, "RestoreButton");
            Check(settings.RestoreText == "Restored", $"Restore purchases reads '{settings.RestoreText}'");
            PlaytestTools.CapturePortraitWithHud("m12_4_ads_removed.png");

            Save.Save();
            Save.Save();
            string backup = ((SaveService)Save).BackupPath;
            Check(File.Exists(backup) && File.ReadAllText(backup).Contains("\"removeAdsOwned\":true"), "the backup save holds the purchase");
            settings.Close();
            AutoTestRunner.RestartPlayModeWithDamagedSave();
        }

        private static void CheckRecoveredSave()
        {
            Check(Save.Recovery == SaveRecovery.FromBackup, $"the damaged save was replaced by the backup ({Save.Recovery})");
            Check(Toast.Text == "Your save was damaged, so the backup was restored.", $"and the player is told: '{Toast.Text}'");
            Check(File.Exists(((SaveService)Save).FilePath + ".corrupt"), "the damaged file is kept aside as .corrupt");
            Check(Privacy.Choice == PrivacyChoice.Accepted && !PrivacyPanel.IsOpen, "the privacy answer survived, so it is not asked again");
            Check(Iap.IsOwned(RemoveAdsId) && PlaytestTools.Find<BoostButtonView>().LabelText.StartsWith("2x"), "and Remove ads is still owned");
            PlaytestTools.CapturePortraitWithHud("m12_5_recovered.png");
        }

        private static void OnPrivacyChanged(PrivacyChanged evt)
        {
            _privacyEvents++;
            _lastAnalytics = evt.AnalyticsAllowed;
        }

        private static void Done()
        {
            EventBus.Unsubscribe<PrivacyChanged>(OnPrivacyChanged);
            _privacyEvents = 0;
            AutoTestRunner.Finish();
        }

        private static void Check(bool condition, string description)
        {
            if (condition) AutoTestRunner.Write("ok: " + description);
            else AutoTestRunner.Fail(description);
        }
    }
}
