#if BEACH_IAP
using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;
using UnityEngine.Purchasing;
using ProductDefinition = UnityEngine.Purchasing.ProductDefinition;

namespace BeachTycoon.Platform
{
    /// <summary>
    /// Google Play and App Store purchases through Unity IAP 5. A purchase is granted and saved before it is
    /// confirmed, so a crash in between re-delivers it instead of losing it. Ownership lives in the save;
    /// purchases fetched at start (a reinstall) and restored ones are granted the same way.
    /// </summary>
    public sealed class UnityIapService : IIapService, IDisposable
    {
        private readonly MetaModel _meta;
        private readonly string _removeAdsId;
        private readonly Action _saveNow;
        private StoreController _store;
        private Action<bool> _purchaseFinished;

        public UnityIapService(MetaModel meta, string removeAdsProductId, Action saveNow)
        {
            _meta = meta;
            _removeAdsId = removeAdsProductId;
            _saveNow = saveNow;
        }

        public bool IsReady { get; private set; }

        public bool IsOwned(string productId) => productId == _removeAdsId && _meta.removeAdsOwned;

        public string PriceText(string productId)
        {
            Product product = IsReady ? _store.GetProductById(productId) : null;
            return product != null ? product.metadata.localizedPriceString : string.Empty;
        }

        /// <summary>Connects to the platform store; events are subscribed first so pending purchases are not missed.</summary>
        public async void Connect()
        {
            _store = UnityIAPServices.StoreController();
            _store.OnStoreConnected += OnConnected;
            _store.OnStoreDisconnected += OnDisconnected;
            _store.OnProductsFetched += OnProductsFetched;
            _store.OnProductsFetchFailed += OnProductsFetchFailed;
            _store.OnPurchasesFetched += OnPurchasesFetched;
            _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            _store.OnPurchasePending += OnPurchasePending;
            _store.OnPurchaseConfirmed += OnPurchaseConfirmed;
            _store.OnPurchaseFailed += OnPurchaseFailed;
            _store.OnPurchaseDeferred += OnPurchaseDeferred;
            try
            {
                await _store.Connect();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Store connection failed: {exception.Message}");
            }
        }

        public void Purchase(string productId, Action<bool> onFinished)
        {
            Product product = IsReady ? _store.GetProductById(productId) : null;
            if (product == null || _purchaseFinished != null)
            {
                onFinished?.Invoke(false);
                return;
            }

            _purchaseFinished = onFinished;
            _store.PurchaseProduct(product);
        }

        public void Restore(Action<bool> onFinished)
        {
            if (_store == null)
            {
                onFinished?.Invoke(false);
                return;
            }

            // Each restored purchase arrives through OnPurchasePending (Apple) or the purchase list (Google).
            _store.RestoreTransactions((success, error) =>
            {
                if (!success) Debug.LogWarning($"Restore failed: {error}");
                else _store.FetchPurchases();
                onFinished?.Invoke(success);
            });
        }

        public void Dispose()
        {
            if (_store == null) return;
            _store.OnStoreConnected -= OnConnected;
            _store.OnStoreDisconnected -= OnDisconnected;
            _store.OnProductsFetched -= OnProductsFetched;
            _store.OnProductsFetchFailed -= OnProductsFetchFailed;
            _store.OnPurchasesFetched -= OnPurchasesFetched;
            _store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            _store.OnPurchasePending -= OnPurchasePending;
            _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;
            _store.OnPurchaseFailed -= OnPurchaseFailed;
            _store.OnPurchaseDeferred -= OnPurchaseDeferred;
        }

        private void OnConnected()
        {
            _store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(_removeAdsId, ProductType.NonConsumable) });
            _store.FetchPurchases();
        }

        private void OnDisconnected(StoreConnectionFailureDescription failure) => Debug.LogWarning($"Store disconnected: {failure.Message}");
        private void OnProductsFetched(List<Product> products) => IsReady = true;
        private void OnProductsFetchFailed(ProductFetchFailed failure) => Debug.LogWarning($"Products not available: {failure.FailureReason}");
        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure) => Debug.LogWarning($"Purchases not fetched: {failure.Message}");

        // Already-confirmed purchases (a reinstall or another device) unlock without a new payment.
        private void OnPurchasesFetched(Orders orders)
        {
            foreach (ConfirmedOrder order in orders.ConfirmedOrders)
            {
                if (Contains(order)) Grant();
            }
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (Contains(order)) Grant();
            _store.ConfirmPurchase(order);
            Finish(true);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is FailedOrder failed) Debug.LogWarning($"Purchase not confirmed: {failed.FailureReason} {failed.Details}");
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            Debug.LogWarning($"Purchase failed: {failed.FailureReason} {failed.Details}");
            Finish(false);
        }

        // Ask to Buy and similar: nothing is granted until the purchase comes back as pending.
        private void OnPurchaseDeferred(DeferredOrder order) => Finish(false);

        private void Grant()
        {
            if (_meta.removeAdsOwned) return;
            _meta.removeAdsOwned = true;
            _saveNow?.Invoke();
        }

        private bool Contains(Order order)
        {
            foreach (CartItem item in order.CartOrdered.Items())
            {
                if (item.Product?.definition?.id == _removeAdsId) return true;
            }

            return false;
        }

        private void Finish(bool success)
        {
            Action<bool> callback = _purchaseFinished;
            _purchaseFinished = null;
            callback?.Invoke(success);
        }
    }
}
#endif
