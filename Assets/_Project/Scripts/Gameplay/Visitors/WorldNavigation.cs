using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Walkable layout for visitors: entry points, relax spots and stand queues, plus straight-line paths that
    /// detour around every building, stand and decoration in the way. No NavMesh; queries do not allocate.
    /// </summary>
    public sealed partial class WorldNavigation : System.IDisposable
    {
        private const int MaxDetours = 6;
        private const float CornerMargin = 0.1f;
        private const float LaneMargin = 0.45f; // about half a towel: towels stay clear of a boardwalk's line

        private readonly WorldLayoutConfig _layout;
        private readonly PlacementService _placement;
        private readonly List<Vector3> _spots = new List<Vector3>();
        private readonly List<bool> _spotTaken = new List<bool>();
        private readonly List<int> _spotPiece = new List<int>(); // the land piece a spot lies on, -1 for the first beach
        private readonly Dictionary<int, Pose> _queueAnchors = new Dictionary<int, Pose>();
        private readonly Dictionary<int, Vector3> _standPositions = new Dictionary<int, Vector3>();
        private readonly VisitorConfig _visitors;
        private readonly List<Rect> _obstacles = new List<Rect>();
        private bool _obstaclesDirty = true;

        public WorldNavigation(WorldLayoutConfig layout, VisitorConfig visitors, PlacementService placement)
        {
            _layout = layout;
            _placement = placement;
            _visitors = visitors;
            CreateRelaxSpots(layout, placement.Map, visitors.randomSeed);
            EventBus.Subscribe<ObjectMoved>(OnPlacementChanged);
            EventBus.Subscribe<DecorationPlaced>(OnDecorationPlaced);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<LotStateChanged>(OnLotChanged);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<ObjectMoved>(OnPlacementChanged);
            EventBus.Unsubscribe<DecorationPlaced>(OnDecorationPlaced);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<LotStateChanged>(OnLotChanged);
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

        /// <summary>Reserves a random free relax spot on open land; returns -1 when the beach is full.</summary>
        public int ReserveSpot(System.Random random)
        {
            int count = _spots.Count;
            int start = count > 0 ? random.Next(count) : 0;
            for (int i = 0; i < count; i++)
            {
                int index = (start + i) % count;
                if (!_spotTaken[index] && (_spotPiece[index] < 0 || _placement.IsPieceOpen(_spotPiece[index])))
                {
                    _spotTaken[index] = true;
                    return index;
                }
            }

            return -1;
        }

        /// <summary>Reserves the free relax spot on open land nearest to a point; -1 when the beach is full.</summary>
        public int ReserveSpotNear(Vector3 point)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _spots.Count; i++)
            {
                if (_spotTaken[i] || (_spotPiece[i] >= 0 && !_placement.IsPieceOpen(_spotPiece[i]))) continue;
                float distance = (_spots[i] - point).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
            }

            if (best >= 0) _spotTaken[best] = true;
            return best;
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

        /// <summary>Where a stand stands in the world (UI pointers such as the tutorial arrow use it).</summary>
        public void RegisterStandPosition(int standIndex, Vector3 position) => _standPositions[standIndex] = position;

        public bool TryGetStandPosition(int standIndex, out Vector3 position) =>
            _standPositions.TryGetValue(standIndex, out position);

        public bool TryGetQueueSlot(int standIndex, int slot, out Vector3 position)
        {
            if (_queueAnchors.TryGetValue(standIndex, out Pose anchor))
            {
                Vector2 offset = QueueLayout.Slot(slot, _visitors);
                position = anchor.position + anchor.right * offset.x + anchor.forward * offset.y;
                position.y = 0f;
                return true;
            }

            position = default;
            return false;
        }

        // The first beach's towel rows (with a lane left free up to each boardwalk), then each later piece's
        // (used once that piece opens).
        private void CreateRelaxSpots(WorldLayoutConfig layout, MapConfig map, int seed)
        {
            var random = new System.Random(seed);
            AddSpots(layout, layout.relaxArea, -1, random);
            if (!LandMap.HasPieces(map)) return;
            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (LandMap.HasBoardwalk(map.pieces[i])) FreeLane(map.pieces[i].boardwalkFrom.x, map.boardwalkWidth * 0.5f + LaneMargin);
            }

            for (int i = 0; i < map.pieces.Length; i++)
            {
                if (map.pieces[i].towelArea.width > 0f) AddSpots(layout, map.pieces[i].towelArea, i, random);
            }
        }

        private void FreeLane(float x, float halfWidth)
        {
            for (int i = _spots.Count - 1; i >= 0; i--)
            {
                if (_spotPiece[i] >= 0 || Mathf.Abs(_spots[i].x - x) >= halfWidth) continue;
                _spots.RemoveAt(i);
                _spotTaken.RemoveAt(i);
                _spotPiece.RemoveAt(i);
            }
        }

        private void AddSpots(WorldLayoutConfig layout, Rect area, int piece, System.Random random)
        {
            float step = layout.relaxSpotSpacing;
            for (float z = area.yMin + step * 0.5f; z < area.yMax; z += step)
            {
                for (float x = area.xMin + step * 0.5f; x < area.xMax; x += step)
                {
                    float jx = ((float)random.NextDouble() * 2f - 1f) * layout.relaxSpotJitter;
                    float jz = ((float)random.NextDouble() * 2f - 1f) * layout.relaxSpotJitter;
                    _spots.Add(new Vector3(x + jx, 0f, z + jz));
                    _spotTaken.Add(false);
                    _spotPiece.Add(piece);
                }
            }
        }
    }
}
