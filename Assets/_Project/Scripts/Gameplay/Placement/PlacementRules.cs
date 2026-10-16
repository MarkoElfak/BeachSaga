using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Pure geometry used by placement: grid snapping, gap-aware overlap and a spiral search.</summary>
    public static class PlacementRules
    {
        public static Vector2 Snap(Vector2 point, float grid)
        {
            if (grid <= 0f) return point;
            return new Vector2(Mathf.Round(point.x / grid) * grid, Mathf.Round(point.y / grid) * grid);
        }

        /// <summary>Rectangle of the given size centred on 'center' (x, z).</summary>
        public static Rect Centered(Vector2 center, Vector2 size) => new Rect(center - size * 0.5f, size);

        /// <summary>Footprint rect relative to an origin (lot footprints are stored relative to the lot).</summary>
        public static Rect Offset(Rect relative, Vector2 origin) => new Rect(relative.position + origin, relative.size);

        /// <summary>
        /// Bounding box of a footprint (relative to its origin) turned by 'yaw' degrees around the origin, the
        /// same way Quaternion.Euler(0, yaw, 0) turns the object on screen.
        /// </summary>
        public static Rect Rotate(Rect local, float yaw)
        {
            if (Mathf.Approximately(Mathf.Repeat(yaw, 360f), 0f)) return local;
            float radians = yaw * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
            float xMin = float.MaxValue, xMax = float.MinValue, zMin = float.MaxValue, zMax = float.MinValue;
            for (int corner = 0; corner < 4; corner++)
            {
                float x = (corner & 1) == 0 ? local.xMin : local.xMax;
                float z = (corner & 2) == 0 ? local.yMin : local.yMax;
                float rx = x * cos + z * sin, rz = -x * sin + z * cos;
                xMin = Mathf.Min(xMin, rx);
                xMax = Mathf.Max(xMax, rx);
                zMin = Mathf.Min(zMin, rz);
                zMax = Mathf.Max(zMax, rz);
            }

            return Rect.MinMaxRect(xMin, zMin, xMax, zMax);
        }

        /// <summary>True when the rectangles overlap or are closer than 'gap'. Touching at exactly 'gap' is allowed.</summary>
        public static bool Overlaps(Rect a, Rect b, float gap)
        {
            return a.xMin < b.xMax + gap && b.xMin < a.xMax + gap && a.yMin < b.yMax + gap && b.yMin < a.yMax + gap;
        }

        /// <summary>True when 'inner' lies completely inside 'outer' shrunk by 'margin'.</summary>
        public static bool Inside(Rect inner, Rect outer, float margin)
        {
            return inner.xMin >= outer.xMin + margin && inner.xMax <= outer.xMax - margin
                && inner.yMin >= outer.yMin + margin && inner.yMax <= outer.yMax - margin;
        }

        /// <summary>
        /// The n-th candidate of a square spiral around 'center' (n = 0 is the centre itself), on a grid of 'step'.
        /// Visiting n = 0, 1, 2 ... walks outwards ring by ring.
        /// </summary>
        public static Vector2 Spiral(Vector2 center, float step, int n)
        {
            if (n == 0) return center;
            int ring = 1;
            int ringStart = 1;
            while (n >= ringStart + ring * 8)
            {
                ringStart += ring * 8;
                ring++;
            }

            int offset = n - ringStart;
            int side = offset / (ring * 2);
            int along = offset % (ring * 2) - ring + 1;
            int x, z;
            switch (side)
            {
                case 0: x = along; z = -ring; break;
                case 1: x = ring; z = along; break;
                case 2: x = -along; z = ring; break;
                default: x = -ring; z = -along; break;
            }

            return center + new Vector2(x, z) * step;
        }

        /// <summary>Number of spiral candidates needed to cover 'radius'.</summary>
        public static int SpiralCount(float radius, float step)
        {
            int rings = Mathf.CeilToInt(radius / Mathf.Max(0.01f, step));
            return (2 * rings + 1) * (2 * rings + 1);
        }
    }
}
