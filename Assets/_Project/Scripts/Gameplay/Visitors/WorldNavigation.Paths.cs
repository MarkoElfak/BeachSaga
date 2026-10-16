using System.Collections.Generic;
using BeachTycoon.Core;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Straight walks with detours around obstacles (footprints grown by the layout's path clearance).</summary>
    public sealed partial class WorldNavigation
    {
        /// <summary>Fills path with waypoints from 'from' to 'to', walking around buildings, stands and props.</summary>
        public void BuildPath(Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            AppendPath(from, to, path);
        }

        /// <summary>Adds the waypoints from 'from' to 'to' after whatever the path already holds.</summary>
        public void AppendPath(Vector3 from, Vector3 to, List<Vector3> path)
        {
            RefreshObstacles();
            Vector3 current = from;
            for (int detour = 0; detour < MaxDetours; detour++)
            {
                if (!TryFirstObstacle(current, to, out Rect rect)) break;
                // Walk around the obstacle along its shorter side, then carry on from its far corner.
                current = AddDetour(current, to, rect, path);
            }

            path.Add(to);
        }

        private void OnPlacementChanged(ObjectMoved evt) => _obstaclesDirty = true;
        private void OnDecorationPlaced(DecorationPlaced evt) => _obstaclesDirty = true;
        private void OnStandCreated(StandCreated evt) => _obstaclesDirty = true;
        private void OnLotChanged(LotStateChanged evt) => _obstaclesDirty = true;

        private void RefreshObstacles()
        {
            if (!_obstaclesDirty) return;
            _obstaclesDirty = false;
            _placement.GetObstacles(_obstacles, _layout.pathClearance);
        }

        // The obstacle the straight segment enters first (ignoring any it starts or ends inside).
        private bool TryFirstObstacle(Vector3 from, Vector3 to, out Rect hit)
        {
            hit = default;
            float best = float.MaxValue;
            for (int i = 0; i < _obstacles.Count; i++)
            {
                Rect rect = _obstacles[i];
                if (Contains(rect, from) || Contains(rect, to) || !SegmentHitsRect(from, to, rect, out float enter)) continue;
                if (enter < best)
                {
                    best = enter;
                    hit = rect;
                }
            }

            return best < float.MaxValue;
        }

        // Adds the two corners of the cheaper side (left/right for mostly vertical walks, bottom/top otherwise).
        private static Vector3 AddDetour(Vector3 from, Vector3 to, Rect r, List<Vector3> path)
        {
            bool vertical = Mathf.Abs(to.z - from.z) >= Mathf.Abs(to.x - from.x);
            Vector3 a, b;
            if (vertical)
            {
                float leftCost = Mathf.Abs(from.x - r.xMin) + Mathf.Abs(to.x - r.xMin);
                float rightCost = Mathf.Abs(from.x - r.xMax) + Mathf.Abs(to.x - r.xMax);
                float x = (leftCost <= rightCost ? r.xMin : r.xMax) + (leftCost <= rightCost ? -CornerMargin : CornerMargin);
                float nearZ = from.z < to.z ? r.yMin - CornerMargin : r.yMax + CornerMargin;
                float farZ = from.z < to.z ? r.yMax + CornerMargin : r.yMin - CornerMargin;
                a = new Vector3(x, 0f, nearZ);
                b = new Vector3(x, 0f, farZ);
            }
            else
            {
                float bottomCost = Mathf.Abs(from.z - r.yMin) + Mathf.Abs(to.z - r.yMin);
                float topCost = Mathf.Abs(from.z - r.yMax) + Mathf.Abs(to.z - r.yMax);
                float z = (bottomCost <= topCost ? r.yMin : r.yMax) + (bottomCost <= topCost ? -CornerMargin : CornerMargin);
                float nearX = from.x < to.x ? r.xMin - CornerMargin : r.xMax + CornerMargin;
                float farX = from.x < to.x ? r.xMax + CornerMargin : r.xMin - CornerMargin;
                a = new Vector3(nearX, 0f, z);
                b = new Vector3(farX, 0f, z);
            }

            path.Add(a);
            path.Add(b);
            return b;
        }

        private static bool Contains(Rect rect, Vector3 point) => rect.Contains(new Vector2(point.x, point.z));

        // Liang-Barsky clip of the XZ segment against the rectangle; 'enter' is where (0..1) it goes in.
        private static bool SegmentHitsRect(Vector3 a, Vector3 b, Rect r, out float enter)
        {
            float t0 = 0f, t1 = 1f;
            float dx = b.x - a.x, dz = b.z - a.z;
            bool hits = Clip(-dx, a.x - r.xMin, ref t0, ref t1) && Clip(dx, r.xMax - a.x, ref t0, ref t1)
                && Clip(-dz, a.z - r.yMin, ref t0, ref t1) && Clip(dz, r.yMax - a.z, ref t0, ref t1);
            enter = t0;
            return hits;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(p, 0f)) return q >= 0f;
            float t = q / p;
            if (p < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
            else { if (t < t0) return false; if (t < t1) t1 = t; }
            return true;
        }
    }
}
