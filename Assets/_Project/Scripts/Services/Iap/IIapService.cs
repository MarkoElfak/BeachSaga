using System;

namespace BeachTycoon.Services
{
    /// <summary>In-app purchase seam. Only the non-consumable "remove ads" exists for now.</summary>
    public interface IIapService
    {
        /// <summary>The store is connected and its products are known (prices can be shown).</summary>
        bool IsReady { get; }

        bool IsOwned(string productId);

        /// <summary>Localised price ("$2.99", "2,99 €"), or empty while the store is not ready.</summary>
        string PriceText(string productId);

        void Purchase(string productId, Action<bool> onFinished);

        /// <summary>Gives back non-consumables bought before (a new phone, a reinstall). Required by Apple.</summary>
        void Restore(Action<bool> onFinished);
    }
}
