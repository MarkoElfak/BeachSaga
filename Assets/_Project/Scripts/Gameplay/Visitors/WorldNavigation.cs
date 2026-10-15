using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Walkable layout for visitors: entry points, relax spots and stand queues, plus straight-line
    /// paths that detour around built lots. No NavMesh; every query is allocation-free.
    /// </summary>
    public sealed class WorldNavigation
    {
        private readonly WorldLayoutConfig _layout;
        private readonly BuildingManager _buildings;
        private readonly List<Vector3> _spots = new List<Vector3>();
        private readonly List<bool> _spotTaken = new List<bool>();
        private readonly Dictionary<int, Pose> _queueAnchors = new Dictionary<int, Pose>();
        private readonly float _queueSpacing;

        public WorldNavigation(WorldLayoutConfig layout, VisitorConfig visitors, BuildingManager buildings)
        {
            _layout = layout;
            _buildings = buildings;
            _queueSpacing = visitors.queueSpacing;
            CreateRelaxSpots(layout, visitors.randomSeed);
        }

        public int EntryCount => _layout.entryPoints != null ? _layout.entryPoints.Length : 0;

        public Vector3 GetEntry(int index)
        {
            Vector2 p = _layout.entryPoints[index % EntryCount];
            return new Vector3(p.x, 0f, p.y);
        }

        /// <summary>Point on the sand edge straight up from an entry.</summary>
        public Vector3 GetBeachAccess(Vector3 entry) => new Vector3(entry.x, 0f, _layout.beachAccessZ);

        public Vector3 GetSpot(int index) => _spots[index];

        /// <summary>Reserves a random free relax spot; returns -1 when the beach is full.</summary>
        public int ReserveSpot(System.Random random)
        {
            int count = _spots.Count;
            int start = count > 0 ? random.Next(count) : 0;
            for (int i = 0; i < count; i++)
            {
                int index = (start + i) % count;
                if (!_spotTaken[index])
                {
                    _spotTaken[index] = true;
                    return index;
                }
            }

            return -1;
        }

        public void ReleaseSpot(int index)
        {
            if (index >= 0 && index < _spotTaken.Count)
            {
                _spotTaken[index] = false;
            }
        }

        /// <summary>Called by StandView once the stand is placed: where its queue starts and which way it grows.</summary>
        public void RegisterQueue(int standIndex, Vector3 anchor, Vector3 direction)
        {
            direction.y = 0f;
            _queueAnchors[standIndex] = new Pose(anchor, Quaternion.LookRotation(direction.normalized));
        }

        public bool TryGetQueueSlot(int standIndex, int slot, out Vector3 position)
        {
            if (_queueAnchors.TryGetValue(standIndex, out Pose anchor))
            {
                position = anchor.position + anchor.forward * (slot * _queueSpacing);
                position.y = 0f;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>Fills path with waypoints from 'from' to 'to', going around any built lot in the way.</summary>
        public void BuildPath(Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                if (_buildings.GetLot(i).state == LotState.Empty || !TryGetObstacle(i, out Rect rect))
                {
                    continue;
                }

                if (SegmentHitsRect(from, to, rect) && !Contains(rect, from) && !Contains(rect, to))
                {
                    // Walk down the closer side of the building, then on to the target.
                    float leftCost = Mathf.Abs(from.x - rect.xMin) + Mathf.Abs(to.x - rect.xMin);
                    float rightCost = Mathf.Abs(from.x - rect.xMax) + Mathf.Abs(to.x - rect.xMax);
                    float sideX = leftCost <= rightCost ? rect.xMin : rect.xMax;
                    path.Add(new Vector3(sideX, 0f, from.z));
                    path.Add(new Vector3(sideX, 0f, to.z));
                    break;
                }
            }

            path.Add(to);
        }

        private bool TryGetObstacle(int lotIndex, out Rect rect)
        {
            rect = default;
            if (_layout.lotPositions == null || lotIndex >= _layout.lotPositions.Length)
            {
                return false;
            }

            Rect footprint = BuildingManager.IsHouseLot(lotIndex) ? _layout.houseFootprint : _layout.mainFootprint;
            float clearance = _layout.pathClearance;
            Vector2 min = _layout.lotPositions[lotIndex] + footprint.position - Vector2.one * clearance;
            rect = new Rect(min, footprint.size + Vector2.one * (clearance * 2f));
            return true;
        }

        private static bool Contains(Rect rect, Vector3 point) => rect.Contains(new Vector2(point.x, point.z));

        // Liang-Barsky clip of the XZ segment against the rectangle.
        private static bool SegmentHitsRect(Vector3 a, Vector3 b, Rect r)
        {
            float t0 = 0f, t1 = 1f;
            float dx = b.x - a.x, dz = b.z - a.z;
            return Clip(-dx, a.x - r.xMin, ref t0, ref t1) && Clip(dx, r.xMax - a.x, ref t0, ref t1)
                && Clip(-dz, a.z - r.yMin, ref t0, ref t1) && Clip(dz, r.yMax - a.z, ref t0, ref t1);
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(p, 0f)) return q >= 0f;
            float t = q / p;
            if (p < 0f) { if (t > t1) return false; if (t > t0) t0 = t; }
            else { if (t < t0) return false; if (t < t1) t1 = t; }
            return true;
        }

        private void CreateRelaxSpots(WorldLayoutConfig layout, int seed)
        {
            var random = new System.Random(seed);
            Rect area = layout.relaxArea;
            float step = layout.relaxSpotSpacing;
            for (float z = area.yMin + step * 0.5f; z < area.yMax; z += step)
            {
                for (float x = area.xMin + step * 0.5f; x < area.xMax; x += step)
                {
                    float jx = ((float)random.NextDouble() * 2f - 1f) * layout.relaxSpotJitter;
                    float jz = ((float)random.NextDouble() * 2f - 1f) * layout.relaxSpotJitter;
                    _spots.Add(new Vector3(x + jx, 0f, z + jz));
                    _spotTaken.Add(false);
                }
            }
        }
    }
}
