using System;

namespace BeachTycoon.Services
{
    /// <summary>In-app purchase seam. Only the non-consumable "remove ads" exists for now.</summary>
    public interface IIapService
    {
        bool IsOwned(string productId);
        void Purchase(string productId, Action<bool> onFinished);
    }
}
