using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Shore-only stands (PlacementConfig.shoreOnlyProducts, e.g. the surf school) must stand at the water: the
    /// sea-side edge of their footprint within shoreDistance of the top of the unlocked land.
    /// </summary>
    public sealed partial class PlacementService
    {
        public bool IsShoreOnly(string productId)
        {
            string[] products = _config.shoreOnlyProducts;
            if (products == null) return false;
            for (int i = 0; i < products.Length; i++)
            {
                if (products[i] == productId) return true;
            }

            return false;
        }

        /// <summary>The footprint reaches the water line closely enough.</summary>
        public bool TouchesShore(Rect footprint) => footprint.yMax >= UnlockedArea.yMax - _config.shoreDistance;

        /// <summary>Shore-only stands standing inland (older saves) move to the nearest free shore spot.</summary>
        public void EnsureShoreStands()
        {
            for (int i = 0; i < _stands.Count; i++)
            {
                PlaceableId id = PlaceableId.Stand(i);
                Vector2 at = GetStandPosition(i);
                if (!IsShoreOnlyStand(id) || TouchesShore(GetFootprint(id, at))) continue;
                if (TryFindFreeSpot(id, ShoreStart(i, at), out Vector2 spot)) SetPosition(id, spot);
            }
        }

        private bool IsShoreOnlyStand(PlaceableId id) =>
            id.Kind == PlaceableKind.Stand && id.Index >= 0 && id.Index < _stands.Count && IsShoreOnly(_stands[id.Index].productId);

        // Where the search for a shore spot starts: the same x, its back just inside the water line.
        private Vector2 ShoreStart(int standIndex, Vector2 near)
        {
            Rect local = StandFootprint(standIndex);
            return new Vector2(near.x, UnlockedArea.yMax - _config.gap - local.yMax);
        }
    }
}
