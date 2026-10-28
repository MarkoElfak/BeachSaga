namespace BeachTycoon.Core
{
    /// <summary>A wedding at the chapel paid out (its coins drop at the chapel like a sale).</summary>
    public readonly struct WeddingHeld
    {
        public readonly int StandIndex;
        public readonly long Amount;

        public WeddingHeld(int standIndex, long amount)
        {
            StandIndex = standIndex;
            Amount = amount;
        }
    }

    /// <summary>The build bar switched tabs (Stands, Food, Services or Decor).</summary>
    public readonly struct BuildTabChanged
    {
        public readonly int Tab;

        public BuildTabChanged(int tab)
        {
            Tab = tab;
        }
    }
}
