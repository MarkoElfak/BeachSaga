using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Validity: a footprint must be on open land (decorations also above the water), off the footpath (up to
    /// where it meets the beach), the towel areas and open boardwalks, and its blocking area (footprint plus a
    /// stand's queue) must keep 'gap' away from every other object's blocking area. Shore-only stands must touch the shore.
    /// </summary>
    public sealed partial class PlacementService
    {
        // A stand also keeps the area in front of it free for its queue lines (toward -z before turning).
        private Rect BlockingRect(PlaceableId id, Vector2 at, float yaw)
        {
            Rect local = LocalFootprint(id);
            if (id.Kind == PlaceableKind.Stand)
            {
                ProductDefinition product = Product(id.Index);
                Vector2 start = product != null ? product.queueStart : Vector2.zero;
                float length = QueueLayout.Length(_database.visitors);
                float halfWidth = QueueLayout.Width(_database.visitors, _config.queueWidth) * 0.5f;
                var queue = Rect.MinMaxRect(start.x - halfWidth, start.y - length, start.x + halfWidth, start.y);
                local = Rect.MinMaxRect(Mathf.Min(local.xMin, queue.xMin), Mathf.Min(local.yMin, queue.yMin),
                    Mathf.Max(local.xMax, queue.xMax), Mathf.Max(local.yMax, queue.yMax));
            }

            return PlacementRules.Offset(PlacementRules.Rotate(local, yaw), at);
        }

        private bool IsFree(Rect footprint, Rect blocking, PlaceableId self)
        {
            if (!IsOnLand(footprint, self.Kind == PlaceableKind.Decoration)) return false;
            if (IsShoreOnlyStand(self) && !TouchesShore(footprint)) return false;

            if (OverlapsTowels(blocking) || OverlapsBoardwalk(blocking)) return false;
            if (PlacementRules.Overlaps(blocking, FootpathArea(), 0f)) return false;

            // New decorations also keep off empty house lots, so neighbours keep their reserved spots.
            bool avoidReserved = self.Kind == PlaceableKind.Decoration && self.Index < 0;
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                PlaceableId other = PlaceableId.Lot(i);
                if (other.Equals(self) || (!avoidReserved && !IsPresent(other))) continue;
                if (Overlaps(blocking, other)) return false;
            }

            for (int i = 0; i < _stands.Count; i++)
            {
                PlaceableId other = PlaceableId.Stand(i);
                if (!other.Equals(self) && Overlaps(blocking, other)) return false;
            }

            for (int i = 0; i < _decorations.Count; i++)
            {
                PlaceableId other = PlaceableId.Decoration(i);
                if (!other.Equals(self) && Overlaps(blocking, other)) return false;
            }

            return true;
        }

        private bool Overlaps(Rect blocking, PlaceableId other)
        {
            Rect otherBlocking = BlockingRect(other, GetPosition(other), GetYaw(other));
            return PlacementRules.Overlaps(blocking, otherBlocking, _config.gap);
        }

        // Buildings and stands level the ground under them, so only decorations check the natural height.
        private bool IsOnLand(Rect rect, bool checkHeight)
        {
            if (!IsOnOpenLand(rect)) return false;
            if (!checkHeight || _terrain == null) return true; // no terrain yet (boot, tests)
            return _terrain.Height(rect.center.x, rect.center.y, false) >= _terrain.WaterLevel + _config.minHeightAboveWater;
        }

        /// <summary>The footpath plus its continuation up to where visitors step onto the beach.</summary>
        private Rect FootpathArea()
        {
            if (_database.terrain == null) return new Rect();
            Rect path = _database.terrain.path;
            float top = Mathf.Max(path.yMax, _database.world.beachAccessZ);
            return Rect.MinMaxRect(path.xMin, path.yMin, path.xMax, top);
        }

        private Rect MainFootprint()
        {
            TierDefinition tier = _database.GetTier(ReservedMainTier());
            return tier != null && tier.footprint.width > 0f ? tier.footprint : FallbackFootprint;
        }

        // An empty main lot reserves the tent; a building one reserves the tier it is turning into.
        private int ReservedMainTier()
        {
            LotModel main = _buildings.GetLot(BuildingManager.MainLotIndex);
            int tier = main.state == LotState.Building ? Mathf.Max(main.tier, main.targetTier) : main.tier;
            return Mathf.Max(1, tier);
        }

        private Rect StandFootprint(int standIndex)
        {
            ProductDefinition product = Product(standIndex);
            return product != null && product.standFootprint.width > 0f ? product.standFootprint : FallbackFootprint;
        }

        private ProductDefinition Product(int standIndex) =>
            standIndex >= 0 && standIndex < _stands.Count ? _database.FindProduct(_stands[standIndex].productId) : null;

        private Rect DecorationArea(string decorationId)
        {
            DecorationDefinition definition = _database.FindDecoration(decorationId);
            return definition != null && definition.area.width > 0f ? definition.area : FallbackFootprint;
        }
    }
}
