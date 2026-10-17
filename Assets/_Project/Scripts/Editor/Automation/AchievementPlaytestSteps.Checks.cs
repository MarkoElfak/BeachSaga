using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using BeachTycoon.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>The individual checks of the M7 playtest.</summary>
    internal static partial class AchievementPlaytestSteps
    {
        private static void CheckFreshStart()
        {
            GameDatabase database = ServiceLocator.Get<GameDatabase>();
            Check(Book.Count == database.achievements.Length && Book.Count >= 10, $"{Book.Count} achievements loaded from data");
            Check(Book.CompletedCount == 0 && Book.ClaimableCount == 0, "a new game has nothing completed");
            MetaModel meta = ServiceLocator.Get<ISaveService>().Data.meta;
            meta.tutorialStep = ServiceLocator.Get<TutorialService>().StepCount;
            EventBus.Publish(new TutorialStepChanged(meta.tutorialStep));
            Transform button = PlaytestTools.Find<Canvas>().transform.Find("SafeArea/TrophyButton");
            Check(button != null && button.gameObject.activeInHierarchy, "Trophies button is on the HUD after the tutorial");
            Check(Badge() == string.Empty, "no badge while nothing is claimable");
            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "TrophyButton");
            Check(Panel.IsOpen && Panel.Row(0) != null, "Trophies panel opens with a row per achievement");
            Check(Panel.Row(Index("Hotel")).Shown == AchievementRowView.State.InProgress, "rows start in progress");
            PlaytestTools.CapturePortraitWithHud("m7_0_trophies_start.png");
            Panel.Close();
        }

        private static void UpgradeDonutStand(int level)
        {
            StandService stands = ServiceLocator.Get<StandService>();
            StandController donuts = stands.FindByProduct("donuts");
            while (donuts.Model.level < level && stands.TryUpgradeStand(donuts)) { }
            Check(donuts.Model.level == level && Book.IsComplete(Index("StandLevel10")), $"donut stand at level {donuts.Model.level} completes Top stand");
        }

        private static void BuyAllDecorations()
        {
            var decorations = ServiceLocator.Get<DecorationService>();
            foreach (DecorationDefinition decoration in ServiceLocator.Get<GameDatabase>().decorations)
            {
                while (decorations.CanBuy(decoration) && decorations.TryBuy(decoration)) { }
            }

            Check(Progress("Decor10") == decorations.Count, $"Decorator counts {decorations.Count} placed decorations");
            Check(decorations.Count < 10 || Book.IsComplete(Index("Decor10")), "ten decorations complete Decorator");
        }

        private static void CheckPanelAndClaims()
        {
            long donuts = Progress("Donuts100");
            Check(donuts >= SessionState.GetInt(Key + "Donuts", 0) + DonutSalesToSee, $"real donut sales count for Donut king ({donuts})");
            int claimable = Book.ClaimableCount;
            Check(Badge() == claimable.ToString(), $"badge shows {claimable} rewards waiting");
            PlaytestTools.Click(PlaytestTools.Find<Canvas>(), "TrophyButton");
            Check(Panel.Row(Index("Hotel")).transform.GetSiblingIndex() < claimable, "claimable achievements are listed first");
            Check(Panel.Row(Index("Coins1M")).Shown == AchievementRowView.State.InProgress, "Millionaire is still in progress");
            PlaytestTools.CapturePortraitWithHud("m7_2_panel_claimable.png");

            long coins = Economy.Coins;
            float boost = ServiceLocator.Get<IncomeBoost>().RemainingSeconds;
            PlaytestTools.Click(Panel.Row(Index("Hotel")), "ClaimButton");
            PlaytestTools.Click(Panel.Row(Index("Coins10K")), "ClaimButton");
            AchievementDefinition hotel = Book.Get(Index("Hotel"));
            long expected = coins + hotel.rewardCoins + Book.Get(Index("Coins10K")).rewardCoins;
            Check(Economy.Coins == expected, $"two claims pay {expected - coins} coins");
            Check(ServiceLocator.Get<IncomeBoost>().RemainingSeconds >= boost + hotel.rewardBoostMinutes * 60f - 1f, "Hotelier adds 15 min of 2x income");
            Check(Book.ClaimableCount == claimable - 2 && Badge() == (claimable > 2 ? (claimable - 2).ToString() : string.Empty), "badge counts down after claiming");
            Check(Panel.Row(Index("Hotel")).Shown == AchievementRowView.State.Claimed, "claimed row shows Done");
            Check(!Achievements.Claim(Index("Hotel")), "a claimed achievement cannot pay twice");
            Check(Analytics.HasLogged(AnalyticsEvents.AchievementClaimed), "analytics: achievement_claimed");
            PlaytestTools.CapturePortraitWithHud("m7_3_after_claim.png");

            Panel.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            PlaytestTools.CapturePortraitWithHud("m7_4_scrolled.png");
            Panel.Close();
        }

        private static void CheckAfterRestart()
        {
            PlaytestTools.Find<DailyRewardPanelView>().Close();
            Check(Book.IsClaimed(Index("Hotel")) && Book.IsClaimed(Index("Coins10K")), "claims survive a restart");
            Check(Book.ClaimableCount == SessionState.GetInt(Key + "Claimable", -1), "waiting rewards survive a restart");
            Check(Progress("Donuts100") >= SessionState.GetInt(Key + "Donuts", 0) && Progress("Visits3") == 1, "progress survives a restart");
            Check(ServiceLocator.Get<ISaveService>().Data.version == GameSaveData.CurrentVersion, $"save is version {GameSaveData.CurrentVersion}");
            ServiceLocator.Get<DailyRewardService>().Claim();
            Check(Progress("Streak7") == 1, "the first daily claim starts Loyal local");
        }

        private static MockAnalyticsService Analytics => (MockAnalyticsService)ServiceLocator.Get<IAnalyticsService>();

        private static string Badge()
        {
            Transform badge = PlaytestTools.Find<Canvas>().transform.Find("SafeArea/TrophyButton/Badge");
            return badge != null && badge.gameObject.activeSelf ? badge.GetComponentInChildren<TMP_Text>().text : string.Empty;
        }

        private static string ToastText()
        {
            ToastView toast = PlaytestTools.Find<ToastView>();
            return toast != null ? toast.GetComponentInChildren<TMP_Text>(true).text : string.Empty;
        }
    }
}
