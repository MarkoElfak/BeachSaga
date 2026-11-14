using System;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Platform
{
    /// <summary>
    /// Creates the real store, analytics and ads services when their Unity packages are installed (version
    /// defines in the asmdef), or returns null so the caller keeps its mock. Nothing else in the game knows
    /// which SDKs exist.
    /// </summary>
    public static class PlatformServices
    {
        /// <param name="saveNow">Called after a purchase is granted and before it is confirmed with the store.</param>
        public static IIapService CreateIap(MetaModel meta, string removeAdsProductId, Action saveNow)
        {
#if BEACH_IAP
            var iap = new UnityIapService(meta, removeAdsProductId, saveNow);
            iap.Connect();
            return iap;
#else
            return null;
#endif
        }

        /// <summary>Rewarded ads through LevelPlay, or null (keep the mock) without the package or the keys.</summary>
        /// <param name="answered">Whether the privacy question was answered; the SDK waits for it.</param>
        public static IAdsService CreateAds(MonetizationConfig config, bool answered, bool personalized)
        {
#if BEACH_LEVELPLAY
            return config.HasAdsKeys ? new LevelPlayAdsService(config.AdsAppKey.Trim(), config.RewardedAdUnit.Trim(), answered, personalized) : null;
#else
            return null;
#endif
        }

        /// <param name="allowed">The player's current privacy answer; later changes arrive as PrivacyChanged.</param>
        public static IAnalyticsService CreateAnalytics(bool allowed)
        {
#if BEACH_ANALYTICS
            return new UnityAnalyticsService(allowed);
#else
            return null;
#endif
        }
    }
}
