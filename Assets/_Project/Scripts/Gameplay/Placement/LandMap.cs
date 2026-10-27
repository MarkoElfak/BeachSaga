using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Questions about the MapConfig's land pieces at a main-building tier: which land is open, the rectangle the
    /// open mainland forms, the islands, and the area each piece's fog covers. No allocation.
    /// </summary>
    public static class LandMap
    {
        public static bool HasPieces(MapConfig map) => map != null && map.pieces != null && map.pieces.Length > 0;

        public static bool IsOpen(in LandPiece piece, int tier) => piece.unlockTier <= Mathf.Max(1, tier);

        /// <summary>The open mainland pieces; the layout keeps every tier's set a single rectangle.</summary>
        public static bool TryMainland(MapConfig map, int tier, out Rect area)
        {
            area = default;
            bool any = false;
            if (!HasPieces(map)) return false;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                LandPiece piece = map.pieces[i];
                if (piece.island || !IsOpen(piece, tier)) continue;
                area = any ? Encapsulate(area, piece.area) : piece.area;
                any = true;
            }

            return any;
        }

        /// <summary>Everything open at this tier (mainland plus open islands): what full zoom-out shows.</summary>
        public static Rect Bounds(MapConfig map, int tier, Rect mainland)
        {
            if (!HasPieces(map)) return mainland;
            Rect bounds = mainland;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (map.pieces[i].island && IsOpen(map.pieces[i], tier)) bounds = Encapsulate(bounds, map.pieces[i].area);
            }

            return bounds;
        }

        /// <summary>The rectangle lies wholly on one open island.</summary>
        public static bool InsideOpenIsland(MapConfig map, int tier, Rect rect)
        {
            int island = IslandContaining(map, rect);
            return island >= 0 && IsOpen(map.pieces[island], tier);
        }

        /// <summary>The island whose buildable area holds the whole rectangle, or -1.</summary>
        public static int IslandContaining(MapConfig map, Rect rect)
        {
            if (!HasPieces(map)) return -1;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (map.pieces[i].island && PlacementRules.Inside(rect, map.pieces[i].area, 0f)) return i;
            }

            return -1;
        }

        /// <summary>The island (its land, beach included) under a point, or -1 for the mainland and the sea.</summary>
        public static int IslandAt(MapConfig map, Vector2 point)
        {
            if (!HasPieces(map)) return -1;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (map.pieces[i].island && Grow(map.pieces[i].area, map.islandBeach).Contains(point)) return i;
            }

            return -1;
        }

        public static bool HasBoardwalk(in LandPiece piece) => piece.island && piece.boardwalkFrom != piece.boardwalkTo;

        /// <summary>Deck area of an island's boardwalk (x, z), or an empty rectangle.</summary>
        public static Rect BoardwalkArea(MapConfig map, in LandPiece piece)
        {
            if (!HasBoardwalk(piece)) return new Rect();
            float half = map.boardwalkWidth * 0.5f;
            Vector2 a = piece.boardwalkFrom, b = piece.boardwalkTo;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x) - half, Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x) + half, Mathf.Max(a.y, b.y));
        }

        /// <summary>What a locked piece's fog covers: its land, beach included for an island (its boardwalk is not built yet).</summary>
        public static Rect FogArea(MapConfig map, int index)
        {
            LandPiece piece = map.pieces[index];
            return piece.island ? Grow(piece.area, map.islandBeach) : piece.area;
        }

        public static Rect Encapsulate(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        public static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);
    }
}
