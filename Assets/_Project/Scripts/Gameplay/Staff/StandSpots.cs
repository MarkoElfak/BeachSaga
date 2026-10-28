using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Where the people running a stand stand: beside the counter on the stand's left (seat 0) or right (seat 1, the
    /// second place at a restaurant), facing the queue. Walks end further out on that side first, so the last straight
    /// step never cuts through the counter.
    /// </summary>
    public sealed class StandSpots
    {
        private const float SpotGap = 0.35f;     // from the counter's side to the person
        private const float ApproachGap = 1.1f;  // where the walk ends before the last straight step in
        private static readonly Vector3 WorkFacing = new Vector3(0.45f, 0f, -1f).normalized; // the queue, mostly the camera
        private static readonly Vector3 RightFacing = new Vector3(-0.45f, 0f, -1f).normalized;

        private readonly StandService _stands;
        private readonly PlacementService _placement;
        private readonly WorldNavigation _navigation;
        private readonly float _clearance;

        public StandSpots(StandService stands, PlacementService placement, WorldNavigation navigation, WorldLayoutConfig world)
        {
            _stands = stands;
            _placement = placement;
            _navigation = navigation;
            _clearance = world.pathClearance;
        }

        /// <summary>True when this move shifts the stand: its own move, or its lot's default row sliding along.</summary>
        public static bool Moves(ObjectMoved evt, StandController stand)
        {
            bool mine = evt.Kind == PlaceableKind.Stand && evt.Index == stand.Index;
            return mine || (evt.Kind == PlaceableKind.Lot && evt.Index == stand.Model.lotIndex && !stand.Model.placed);
        }

        /// <summary>Beside the counter at a seat (0 = left, 1 = right), and the way to face there.</summary>
        public Vector3 Work(int standIndex, int seat, out Vector3 facing)
        {
            Rect footprint = _stands.Get(standIndex).Product.standFootprint;
            Quaternion rotation = _placement.GetRotation(PlaceableId.Stand(standIndex));
            facing = rotation * (seat == 0 ? WorkFacing : RightFacing);
            return StandPoint(standIndex, rotation, new Vector3(Side(footprint, seat, SpotGap), 0f, footprint.center.y - 0.1f));
        }

        /// <summary>The last seat at a stand (the character takes it, so workers fill the seats from the left).</summary>
        public int LastSeat(int standIndex) => Mathf.Max(0, _stands.Get(standIndex).Product.workerSlots - 1);

        /// <summary>Fills path with a walk from 'from' to a seat at the stand (around everything in the way).</summary>
        public void BuildWalk(Vector3 from, int standIndex, int seat, List<Vector3> path, out Vector3 facing)
        {
            Vector3 spot = Work(standIndex, seat, out facing);
            _navigation.BuildPath(from, Approach(standIndex, seat), path);
            path.Add(spot);
        }

        /// <summary>In front of the main building, for someone with no stand to go to.</summary>
        public Vector3 Waiting(float sideways = 0f)
        {
            var main = PlaceableId.Lot(BuildingManager.MainLotIndex);
            Rect footprint = _placement.GetFootprint(main, _placement.GetLotPosition(BuildingManager.MainLotIndex));
            return new Vector3(footprint.xMin - SpotGap + sideways, 0f, footprint.yMin - SpotGap);
        }

        private Vector3 Approach(int standIndex, int seat)
        {
            Rect footprint = _stands.Get(standIndex).Product.standFootprint;
            Quaternion rotation = _placement.GetRotation(PlaceableId.Stand(standIndex));
            float side = Side(footprint, seat, SpotGap + _clearance + ApproachGap);
            return StandPoint(standIndex, rotation, new Vector3(side, 0f, footprint.center.y - 0.1f));
        }

        private static float Side(Rect footprint, int seat, float gap) => seat == 0 ? footprint.xMin - gap : footprint.xMax + gap;

        private Vector3 StandPoint(int standIndex, Quaternion rotation, Vector3 local)
        {
            Vector2 at = _placement.GetStandPosition(standIndex);
            return new Vector3(at.x, 0f, at.y) + rotation * local;
        }
    }
}
