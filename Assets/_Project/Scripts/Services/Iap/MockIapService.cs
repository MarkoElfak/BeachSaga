using System;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>No-op store: every purchase succeeds instantly. Ownership of remove-ads lives in the save.</summary>
    public sealed class MockIapService : IIapService
    {
        private readonly MetaModel _meta;
        private readonly string _removeAdsId;
        private readonly string _price;

        public MockIapService(MetaModel meta, string removeAdsProductId, string price = "$2.99")
        {
            _meta = meta;
            _removeAdsId = removeAdsProductId;
            _price = price;
        }

        public bool IsReady => true;

        public bool IsOwned(string productId) => productId == _removeAdsId && _meta.removeAdsOwned;

        public string PriceText(string productId) => productId == _removeAdsId ? _price : string.Empty;

        public void Purchase(string productId, Action<bool> onFinished)
        {
            bool known = productId == _removeAdsId;
            if (known)
            {
                _meta.removeAdsOwned = true;
            }

            onFinished?.Invoke(known);
        }

        public void Restore(Action<bool> onFinished) => onFinished?.Invoke(true);
    }
}
