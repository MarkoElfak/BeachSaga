using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>
    /// Rewarded ad rewards, store product ids and the privacy policy link. Builds use the real store and
    /// analytics when their packages are installed; the Editor uses mocks unless storeServicesInEditor is on.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Monetization", fileName = "MonetizationConfig")]
    public sealed class MonetizationConfig : ScriptableObject
    {
        [Header("Rewarded: income boost")]
        [Min(1f)] public float incomeBoostMultiplier = 2f;
        [Min(1f)] public float incomeBoostMinutes = 10f;

        [Header("Rewarded: offline earnings")]
        [Min(1f)] public float offlineRewardMultiplier = 2f;

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
    }
}
