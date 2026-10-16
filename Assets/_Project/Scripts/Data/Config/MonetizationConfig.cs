using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Rewarded ad rewards and store product ids. Ads and IAP are mocks until a real SDK is added.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Monetization", fileName = "MonetizationConfig")]
    public sealed class MonetizationConfig : ScriptableObject
    {
        [Header("Rewarded: income boost")]
        [Min(1f)] public float incomeBoostMultiplier = 2f;
        [Min(1f)] public float incomeBoostMinutes = 10f;

        [Header("Rewarded: offline earnings")]
        [Min(1f)] public float offlineRewardMultiplier = 2f;

        [Header("Mocks")]
        [Tooltip("How long the mock rewarded ad 'plays' before granting the reward.")]
        [Min(0f)] public float mockAdSeconds = 0.5f;
        public string removeAdsProductId = "remove_ads";
        [Tooltip("Mock analytics also prints every event to the Console.")]
        public bool logAnalyticsToConsole = true;
    }
}
