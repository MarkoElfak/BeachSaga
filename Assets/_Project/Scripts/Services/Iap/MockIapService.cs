using System;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>No-op store: every purchase succeeds instantly. Ownership of remove-ads lives in the save.</summary>
    public sealed class MockIapService : IIapService
    {
        private readonly MetaModel _meta;
        private readonly string _removeAdsId;

        public MockIapService(MetaModel meta, string removeAdsProductId)
        {
            _meta = meta;
            _removeAdsId = removeAdsProductId;
        }

        public bool IsOwned(string productId) => productId == _removeAdsId && _meta.removeAdsOwned;

        public void Purchase(string productId, Action<bool> onFinished)
        {
            bool known = productId == _removeAdsId;
            if (known)
            {
                _meta.removeAdsOwned = true;
            }

            onFinished?.Invoke(known);
        }
    }
}
