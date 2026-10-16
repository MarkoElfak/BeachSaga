using BeachTycoon.Core;

namespace BeachTycoon.Gameplay
{
    /// <summary>Identifies something the player can move: a lot (building or house), a stand or a decoration.</summary>
    public readonly struct PlaceableId
    {
        public readonly PlaceableKind Kind;
        public readonly int Index;

        private PlaceableId(PlaceableKind kind, int index)
        {
            Kind = kind;
            Index = index;
        }

        public static PlaceableId Lot(int index) => new PlaceableId(PlaceableKind.Lot, index);
        public static PlaceableId Stand(int index) => new PlaceableId(PlaceableKind.Stand, index);
        public static PlaceableId Decoration(int index) => new PlaceableId(PlaceableKind.Decoration, index);

        /// <summary>A decoration that is about to be bought and has no index yet.</summary>
        public static PlaceableId NewDecoration => new PlaceableId(PlaceableKind.Decoration, -1);

        public bool IsLot => Kind == PlaceableKind.Lot;

        public bool Equals(PlaceableId other) => Kind == other.Kind && Index == other.Index;

        public override string ToString() => $"{Kind} {Index}";
    }
}
