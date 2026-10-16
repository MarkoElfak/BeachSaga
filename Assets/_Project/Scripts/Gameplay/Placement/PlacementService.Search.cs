using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Free-spot searches: used when a default position is taken (new decorations, new houses).</summary>
    public sealed partial class PlacementService
    {
        /// <summary>Nearest valid grid position to 'near' (spiral search), or false when the area is full.</summary>
        public bool TryFindFreeSpot(PlaceableId id, Vector2 near, out Vector2 spot)
        {
            Vector2 start = PlacementRules.Snap(near, _config.gridSize);
            int candidates = PlacementRules.SpiralCount(_config.searchRadius, _config.gridSize);
            for (int n = 0; n < candidates; n++)
            {
                spot = PlacementRules.Spiral(start, _config.gridSize, n);
                if (CanPlace(id, spot)) return true;
            }

            spot = near;
            return false;
        }

        public bool TryFindFreeSpotForNewDecoration(DecorationDefinition definition, Vector2 near, out Vector2 spot)
        {
            Vector2 start = PlacementRules.Snap(near, _config.gridSize);
            int candidates = PlacementRules.SpiralCount(_config.searchRadius, _config.gridSize);
            for (int n = 0; n < candidates; n++)
            {
                spot = PlacementRules.Spiral(start, _config.gridSize, n);
                if (CanPlaceNewDecoration(definition, spot)) return true;
            }

            spot = near;
            return false;
        }

        /// <summary>
        /// Before a house goes up on an empty lot, make sure the player has not moved something onto its spot;
        /// if so, the lot is relocated to the nearest free place.
        /// </summary>
        public bool EnsureLotIsFree(int lotIndex)
        {
            PlaceableId id = PlaceableId.Lot(lotIndex);
            Vector2 position = GetLotPosition(lotIndex);
            if (CanPlace(id, position)) return true;
            if (!TryFindFreeSpot(id, position, out Vector2 spot)) return false;
            Commit(id, spot);
            return true;
        }
    }
}
