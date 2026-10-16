namespace BeachTycoon.Core
{
    /// <summary>A pooled visitor became active (Id is its pool slot).</summary>
    public readonly struct VisitorSpawned
    {
        public readonly int VisitorId;

        public VisitorSpawned(int visitorId)
        {
            VisitorId = visitorId;
        }
    }

    /// <summary>A visitor went home and its slot returned to the pool.</summary>
    public readonly struct VisitorDespawned
    {
        public readonly int VisitorId;

        public VisitorDespawned(int visitorId)
        {
            VisitorId = visitorId;
        }
    }

    /// <summary>A visitor ran out of patience in a queue and left without buying.</summary>
    public readonly struct VisitorLeftUnserved
    {
        public readonly int VisitorId;

        public VisitorLeftUnserved(int visitorId)
        {
            VisitorId = visitorId;
        }
    }

    /// <summary>A visitor completed a purchase; Purchases is their running total this visit.</summary>
    public readonly struct VisitorPurchased
    {
        public readonly int VisitorId;
        public readonly int Purchases;

        public VisitorPurchased(int visitorId, int purchases)
        {
            VisitorId = visitorId;
            Purchases = purchases;
        }
    }
}
