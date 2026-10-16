namespace BeachTycoon.Core
{
    /// <summary>A lot (IsLot) or a decoration was moved to a new position, or a lot became occupied.</summary>
    public readonly struct ObjectMoved
    {
        public readonly bool IsLot;
        public readonly int Index;

        public ObjectMoved(bool isLot, int index)
        {
            IsLot = isLot;
            Index = index;
        }
    }

    /// <summary>The player picked something up to move it.</summary>
    public readonly struct MoveStarted
    {
    }

    /// <summary>A move finished. Committed is false when the object went back to where it was.</summary>
    public readonly struct MoveEnded
    {
        public readonly bool Committed;

        public MoveEnded(bool committed)
        {
            Committed = committed;
        }
    }
}
