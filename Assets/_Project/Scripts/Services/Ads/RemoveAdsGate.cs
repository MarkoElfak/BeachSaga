using System;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Rewarded ads for a player who bought "remove ads": the reward is granted at once, without a video.
    /// Everyone else gets the real (or mock) ad.
    /// </summary>
    public sealed class RemoveAdsGate : IAdsService
    {
        private readonly IAdsService _ads;
        private readonly IIapService _iap;
        private readonly string _removeAdsId;

        public RemoveAdsGate(IAdsService ads, IIapService iap, string removeAdsProductId)
        {
            _ads = ads;
            _iap = iap;
            _removeAdsId = removeAdsProductId;
        }

        public bool AdsRemoved => _iap.IsOwned(_removeAdsId);
        public bool IsRewardedReady => AdsRemoved || _ads.IsRewardedReady;

        public void ShowRewarded(string placement, Action<bool> onFinished)
        {
            if (AdsRemoved) onFinished?.Invoke(true);
            else _ads.ShowRewarded(placement, onFinished);
        }
    }
}
