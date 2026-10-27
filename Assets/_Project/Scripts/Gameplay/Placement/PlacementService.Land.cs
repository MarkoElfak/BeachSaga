using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// The map's land pieces: the open mainland is one rectangle (UnlockedArea), open islands are extra land, and
    /// every piece's towel area and every open boardwalk are kept free like the footpath.
    /// </summary>
    public sealed partial class PlacementService
    {
        public MapConfig Map => _database.map;

        /// <summary>All open land at the current tier, islands included (what full zoom-out shows).</summary>
        public Rect UnlockedBounds => BoundsForTier(_buildings.MainTier);

        public Rect BoundsForTier(int tierNumber) => LandMap.Bounds(_database.map, tierNumber, AreaForTier(tierNumber));

        public bool IsPieceOpen(int piece) =>
            LandMap.HasPieces(_database.map) && piece >= 0 && piece < _database.map.pieces.Length
            && LandMap.IsOpen(_database.map.pieces[piece], _buildings.MainTier);

        /// <summary>The footprint stands wholly on open land: inside the open mainland or on one open island.</summary>
        private bool IsOnOpenLand(Rect rect) =>
            PlacementRules.Inside(rect, UnlockedArea, 0f) || LandMap.InsideOpenIsland(_database.map, _buildings.MainTier, rect);

        private bool OverlapsTowels(Rect blocking)
        {
            if (PlacementRules.Overlaps(blocking, _database.world.relaxArea, 0f)) return true;
            MapConfig map = _database.map;
            if (!LandMap.HasPieces(map)) return false;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                Rect towels = map.pieces[i].towelArea;
                if (towels.width > 0f && PlacementRules.Overlaps(blocking, towels, 0f)) return true;
            }

            return false;
        }

        private bool OverlapsBoardwalk(Rect blocking)
        {
            MapConfig map = _database.map;
            if (!LandMap.HasPieces(map)) return false;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                LandPiece piece = map.pieces[i];
                if (!LandMap.HasBoardwalk(piece) || !LandMap.IsOpen(piece, _buildings.MainTier)) continue;
                if (PlacementRules.Overlaps(blocking, LandMap.BoardwalkArea(map, piece), 0f)) return true;
            }

            return false;
        }

        // On an island every edge is shore.
        private bool TouchesIslandShore(Rect footprint)
        {
            int island = LandMap.IslandContaining(_database.map, footprint);
            if (island < 0) return false;
            Rect land = _database.map.pieces[island].area;
            float reach = _config.shoreDistance;
            return footprint.yMax >= land.yMax - reach || footprint.yMin <= land.yMin + reach
                || footprint.xMin <= land.xMin + reach || footprint.xMax >= land.xMax - reach;
        }
    }
}
