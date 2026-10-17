using BeachTycoon.Services;

namespace BeachTycoon.UI
{
    /// <summary>Button wording for rewarded ads: a player who bought "remove ads" is not told to watch one.</summary>
    public static class AdWording
    {
        public static bool AdsRemoved(IAdsService ads) => ads is RemoveAdsGate gate && gate.AdsRemoved;

        /// <summary>"Watch ad", or "Free" when ads are removed (the reward is then granted straight away).</summary>
        public static string Watch(IAdsService ads) => AdsRemoved(ads) ? "Free" : "Watch ad";
    }
}
