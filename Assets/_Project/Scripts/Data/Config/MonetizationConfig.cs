using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>
    /// Rewarded ad rewards, the ads keys, store product ids and the privacy policy link. Builds use the real store,
    /// analytics and ads when their packages are installed (ads also need their keys); the Editor uses mocks unless
    /// storeServicesInEditor is on.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Monetization", fileName = "MonetizationConfig")]
    public sealed class MonetizationConfig : ScriptableObject
    {
        [Header("Rewarded: income boost")]
        [Min(1f)] public float incomeBoostMultiplier = 2f;
        [Min(1f)] public float incomeBoostMinutes = 10f;

        [Header("Rewarded: offline earnings")]
        [Min(1f)] public float offlineRewardMultiplier = 2f;

        [Header("Rewarded ads (LevelPlay dashboard)")]
        [Tooltip("Apps > your app: the app key. Empty keeps the mock ads on that platform.")]
        public string androidAppKey = "";
        public string iosAppKey = "";
        [Tooltip("Ad units > your app: the rewarded ad unit id.")]
        public string androidRewardedAdUnit = "";
        public string iosRewardedAdUnit = "";

        [Header("Store and privacy")]
        public string removeAdsProductId = "remove_ads";
        [Tooltip("Shown on the privacy question and in Settings; required by both app stores.")]
        public string privacyPolicyUrl = "";
        [Tooltip("Use the real Unity IAP and Analytics in Play mode too (they always run in builds).")]
        public bool storeServicesInEditor;

        [Header("Mocks")]
        [Tooltip("How long the mock rewarded ad 'plays' before granting the reward.")]
        [Min(0f)] public float mockAdSeconds = 0.5f;
        public string mockRemoveAdsPrice = "$2.99";
        [Tooltip("Mock analytics also prints every event to the Console.")]
        public bool logAnalyticsToConsole = true;

#if UNITY_IOS
        public string AdsAppKey => iosAppKey;
        public string RewardedAdUnit => iosRewardedAdUnit;
#else
        public string AdsAppKey => androidAppKey;
        public string RewardedAdUnit => androidRewardedAdUnit;
#endif

        /// <summary>Real ads need both the app key and the rewarded ad unit for this platform.</summary>
        public bool HasAdsKeys => !string.IsNullOrWhiteSpace(AdsAppKey) && !string.IsNullOrWhiteSpace(RewardedAdUnit);
    }
}
