using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Something the player can long-press and drag to a new spot. MoveController finds it on the hit collider
    /// or a parent, moves MoveRoot while dragging, and PlacementService commits the final position.
    /// </summary>
    public interface IMovable
    {
        PlaceableId Placeable { get; }

        /// <summary>False while the object should stay put (e.g. an empty house reservation).</summary>
        bool CanMoveNow { get; }

        Transform MoveRoot { get; }
    }
}
