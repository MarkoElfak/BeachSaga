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

    /// <summary>A main-building upgrade opened more land. Area is the whole buildable rectangle (x, z).</summary>
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
}
