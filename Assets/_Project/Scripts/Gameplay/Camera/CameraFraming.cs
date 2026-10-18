using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Pure camera math: how far back a tilted perspective camera must sit to see a whole area.</summary>
    public static class CameraFraming
    {
        /// <summary>
        /// Smallest distance from the area's centre (along the view direction) at which every corner of the
        /// area, on the ground and raised by <paramref name="height"/>, is inside the view.
        /// </summary>
        public static float FitDistance(Rect area, Quaternion rotation, float verticalFov, float aspect, float height)
        {
            float tanY = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
            float tanX = tanY * Mathf.Max(0.01f, aspect);
            Quaternion toView = Quaternion.Inverse(rotation);
            Vector3 centre = new Vector3(area.center.x, 0f, area.center.y);
            float distance = 0f;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? area.xMin : area.xMax, (i & 4) == 0 ? 0f : height, (i & 2) == 0 ? area.yMin : area.yMax);
                // In view space the camera sits at (0, 0, -distance) and looks along +z.
                Vector3 local = toView * (corner - centre);
                distance = Mathf.Max(distance, Mathf.Abs(local.x) / tanX - local.z, Mathf.Abs(local.y) / tanY - local.z);
            }

            return distance;
        }

        /// <summary>
        /// Where the focus may go at this zoom: the land plus a look past its edges when close, shrinking to the
        /// land's centre at full zoom-out so the whole map is in view there.
        /// </summary>
        public static Rect FocusBounds(Rect area, float edgeLook, float zoomOut)
        {
            Rect wide = Rect.MinMaxRect(area.xMin - edgeLook, area.yMin - edgeLook, area.xMax + edgeLook, area.yMax + edgeLook);
            float shrink = Mathf.Clamp01(zoomOut);
            shrink *= shrink; // free roaming for most of the range, centred only near the end
            Vector2 centre = area.center;
            return Rect.MinMaxRect(
                Mathf.Lerp(wide.xMin, centre.x, shrink), Mathf.Lerp(wide.yMin, centre.y, shrink),
                Mathf.Lerp(wide.xMax, centre.x, shrink), Mathf.Lerp(wide.yMax, centre.y, shrink));
        }

        /// <summary>
        /// A rubber-band limit: inside [min, max] unchanged; past a limit it eases towards an extra
        /// <paramref name="giveBelow"/> or <paramref name="giveAbove"/> and never quite gets there.
        /// </summary>
        public static float Soften(float value, float min, float max, float giveBelow, float giveAbove)
        {
            if (value > max) return giveAbove > 0f ? max + giveAbove * (1f - Mathf.Exp(-(value - max) / giveAbove)) : max;
            if (value < min) return giveBelow > 0f ? min - giveBelow * (1f - Mathf.Exp(-(min - value) / giveBelow)) : min;
            return value;
        }
    }
}
