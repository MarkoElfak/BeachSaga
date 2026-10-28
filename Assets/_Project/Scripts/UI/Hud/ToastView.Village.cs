using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;

namespace BeachTycoon.UI
{
    /// <summary>Village news: weddings at the chapel, the first-aid station growing into a hospital.</summary>
    public sealed partial class ToastView
    {
        private void SubscribeVillage(bool on)
        {
            if (on)
            {
                EventBus.Subscribe<WeddingHeld>(OnWedding);
                EventBus.Subscribe<TierUpgraded>(OnVillageGrew);
            }
            else
            {
                EventBus.Unsubscribe<WeddingHeld>(OnWedding);
                EventBus.Unsubscribe<TierUpgraded>(OnVillageGrew);
            }
        }

        private void OnWedding(WeddingHeld evt) => Show($"A wedding at the chapel! The guests paid {NumberFormatter.Format(evt.Amount)}.");

        private void OnVillageGrew(TierUpgraded evt)
        {
            if (evt.LotIndex != BuildingManager.MainLotIndex || !ServiceLocator.TryGet(out StandService stands)) return;
            for (int i = 0; i < stands.Count; i++)
            {
                ProductDefinition product = stands.Get(i).Product;
                if (product.IsService && product.service.grownTier == evt.Tier) Show($"Your {product.displayName} grew into a {product.NameAt(evt.Tier)}!");
            }
        }
    }
}
