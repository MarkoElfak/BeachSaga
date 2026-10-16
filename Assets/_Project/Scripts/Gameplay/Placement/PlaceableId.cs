namespace BeachTycoon.Gameplay
{
    /// <summary>Identifies something the player can move: a lot (building or house) or a decoration.</summary>
    public readonly struct PlaceableId
    {
        public readonly bool IsLot;
        public readonly int Index;

        private PlaceableId(bool isLot, int index)
        {
            IsLot = isLot;
            Index = index;
        }

        public static PlaceableId Lot(int index) => new PlaceableId(true, index);
        public static PlaceableId Decoration(int index) => new PlaceableId(false, index);

        /// <summary>A decoration that is about to be bought and has no index yet.</summary>
        public static PlaceableId NewDecoration => new PlaceableId(false, -1);

        public bool Equals(PlaceableId other) => IsLot == other.IsLot && Index == other.Index;
    }
}
