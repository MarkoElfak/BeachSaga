using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Chooses the stand a visitor queues at: a running event's product first (by its demand chance),
    /// then the visitor type's preferred products, then any stand with room, starting at a random one.
    /// </summary>
    public sealed class StandPicker
    {
        private readonly StandService _stands;
        private readonly System.Random _random;
        private readonly BeachEventService _events;

        public StandPicker(StandService stands, System.Random random, BeachEventService events)
        {
            _stands = stands;
            _random = random;
            _events = events;
        }

        /// <summary>A stand with room in its queue, or null when every stand is full.</summary>
        public StandController Pick(VisitorTypeDefinition type)
        {
            StandController wanted = EventDemand();
            if (wanted != null) return wanted;

            if (type.preferredProducts != null)
            {
                foreach (ProductDefinition product in type.preferredProducts)
                {
                    StandController preferred = product != null ? _stands.FindByProduct(product.id) : null;
                    if (preferred != null && !preferred.IsFull) return preferred;
                }
            }

            int count = _stands.Count;
            int start = count > 0 ? _random.Next(count) : 0;
            for (int i = 0; i < count; i++)
            {
                StandController stand = _stands.Get((start + i) % count);
                if (!stand.IsFull) return stand;
            }

            return null;
        }

        // Rolls only while an event adds demand, so the usual visitor choices are unchanged otherwise.
        private StandController EventDemand()
        {
            ProductDefinition product = _events != null ? _events.DemandProduct : null;
            if (product == null || _random.NextDouble() >= _events.DemandChance) return null;
            StandController stand = _stands.FindByProduct(product.id);
            return stand != null && !stand.IsFull ? stand : null;
        }
    }
}
