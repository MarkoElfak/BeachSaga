namespace BeachTycoon.Core
{
    /// <summary>A repeat visitor asked to move in. Answer through NeighbourService.Accept / Decline.</summary>
    public readonly struct NeighbourRequested
    {
        public readonly string Name;

        public NeighbourRequested(string name)
        {
            Name = name;
        }
    }

    /// <summary>The pending move-in request was answered or timed out.</summary>
    public readonly struct NeighbourRequestClosed
    {
        public readonly bool Accepted;

        public NeighbourRequestClosed(bool accepted)
        {
            Accepted = accepted;
        }
    }

    /// <summary>A neighbour moved into a house on a lot.</summary>
    public readonly struct NeighbourMovedIn
    {
        public readonly int NeighbourIndex;
        public readonly int LotIndex;

        public NeighbourMovedIn(int neighbourIndex, int lotIndex)
        {
            NeighbourIndex = neighbourIndex;
            LotIndex = lotIndex;
        }
    }

    /// <summary>A house produced rent; the lot view drops it as a coin pickup.</summary>
    public readonly struct RentDue
    {
        public readonly int LotIndex;
        public readonly long Amount;

        public RentDue(int lotIndex, long amount)
        {
            LotIndex = lotIndex;
            Amount = amount;
        }
    }

    /// <summary>A neighbour's quest moved forward.</summary>
    public readonly struct QuestProgressed
    {
        public readonly int NeighbourIndex;

        public QuestProgressed(int neighbourIndex)
        {
            NeighbourIndex = neighbourIndex;
        }
    }

    /// <summary>A neighbour's quest was completed and its reward paid.</summary>
    public readonly struct QuestCompleted
    {
        public readonly int NeighbourIndex;
        public readonly long Reward;

        public QuestCompleted(int neighbourIndex, long reward)
        {
            NeighbourIndex = neighbourIndex;
            Reward = reward;
        }
    }

    /// <summary>A decoration was bought and placed (index into the decoration list).</summary>
    public readonly struct DecorationPlaced
    {
        public readonly int Index;

        public DecorationPlaced(int index)
        {
            Index = index;
        }
    }

    /// <summary>The player tapped a neighbour's house.</summary>
    public readonly struct HouseTapped
    {
        public readonly int LotIndex;

        public HouseTapped(int lotIndex)
        {
            LotIndex = lotIndex;
        }
    }
}
