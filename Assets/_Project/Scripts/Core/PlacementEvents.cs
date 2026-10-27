using UnityEngine;

namespace BeachTycoon.Core
{
    /// <summary>What kind of movable object a placement event is about.</summary>
    public enum PlaceableKind
    {
        Lot = 0,        // main building or a neighbour's house
        Stand = 1,      // a product stand
        Decoration = 2,
    }

    /// <summary>An object was moved (by the player or to make room), or a lot changed the space it takes.</summary>
    public readonly struct ObjectMoved
    {
        public readonly PlaceableKind Kind;
        public readonly int Index;

        public ObjectMoved(PlaceableKind kind, int index)
        {
            Kind = kind;
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

    /// <summary>A main-building upgrade opened more land. Area holds all open land, islands included (x, z).</summary>
    public readonly struct AreaUnlocked
    {
        public readonly int Tier;
        public readonly Rect Area;

        public AreaUnlocked(int tier, Rect area)
        {
            Tier = tier;
            Area = area;
        }
    }

    /// <summary>A piece of the map (MapConfig.pieces[Piece]) opened: its fog rolls away and the camera flies over it.</summary>
    public readonly struct LandOpened
    {
        public readonly int Piece;
        public readonly Rect Area;

        public LandOpened(int piece, Rect area)
        {
            Piece = piece;
            Area = area;
        }
    }

    /// <summary>The player tapped the fog over a locked piece of the map.</summary>
    public readonly struct LockedLandTapped
    {
        public readonly int Piece;

        public LockedLandTapped(int piece)
        {
            Piece = piece;
        }
    }

    /// <summary>The selected object changed (tap to select, tap the ground to clear). HasSelection false = cleared.</summary>
    public readonly struct SelectionChanged
    {
        public readonly bool HasSelection;

        public SelectionChanged(bool hasSelection)
        {
            HasSelection = hasSelection;
        }
    }

    /// <summary>A rotate request was refused because the turned object would not fit.</summary>
    public readonly struct RotateBlocked
    {
    }
}
