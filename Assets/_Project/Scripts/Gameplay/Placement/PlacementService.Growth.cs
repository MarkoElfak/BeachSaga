using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// How the beach changes as the game goes on: land unlocked per main-building tier, the default row of stands
    /// beside the main building, and making room when the building grows into something bigger.
    /// </summary>
    public sealed partial class PlacementService
    {
        /// <summary>Buildable land at the current main-building tier (x, z, width, depth).</summary>
        public Rect UnlockedArea => AreaForTier(_buildings.MainTier);

        public Rect AreaForTier(int tierNumber)
        {
            for (int tier = Mathf.Min(Mathf.Max(1, tierNumber), _database.MaxTier); tier >= 1; tier--)
            {
                TierDefinition definition = _database.GetTier(tier);
                if (definition != null && definition.buildArea.width > 0f) return definition.buildArea;
            }

            return LandRect(); // no areas configured: the whole island
        }

        /// <summary>
        /// Slot i of the stand row: even slots to the right of the main building, odd ones to the left, each one
        /// a stand-width further out. Follows the building's current (or reserved) footprint.
        /// </summary>
        public Vector2 DefaultStandPosition(int standIndex)
        {
            StandModel model = _stands[standIndex];
            Vector2 lot = GetLotPosition(model.lotIndex);
            Rect building = GetFootprint(PlaceableId.Lot(model.lotIndex), lot);
            WorldLayoutConfig layout = _database.world;
            float width = StandSlotWidth();
            int rank = model.slotIndex / 2;
            float offset = layout.standGap + rank * (width + layout.standGap) + width * 0.5f;
            float centre = model.slotIndex % 2 == 0 ? building.xMax + offset : building.xMin - offset;
            return new Vector2(centre - StandFootprint(standIndex).center.x, lot.y + layout.standRowZ);
        }

        private float StandSlotWidth()
        {
            float width = 0f;
            foreach (ProductDefinition product in _database.products ?? new ProductDefinition[0])
            {
                if (product != null) width = Mathf.Max(width, product.standFootprint.width);
            }

            return width > 0f ? width : FallbackFootprint.width;
        }

        private Rect LandRect()
        {
            WorldLayoutConfig layout = _database.world;
            float depth = 0f;
            foreach (BandSpec band in layout.bands ?? new BandSpec[0]) depth += band.depth;
            float half = layout.width * 0.5f;
            return Rect.MinMaxRect(-half, layout.landTopZ - depth, half, layout.landTopZ);
        }

        // Stands that still follow the building keep their current places from now on.
        private void PinStands(int lotIndex)
        {
            for (int i = 0; i < _stands.Count; i++)
            {
                StandModel stand = _stands[i];
                if (stand.placed || stand.lotIndex != lotIndex) continue;
                Vector2 at = DefaultStandPosition(i);
                stand.placed = true;
                stand.x = at.x;
                stand.z = at.y;
            }
        }

        private void OnStandCreated(StandCreated evt)
        {
            PlaceableId stand = PlaceableId.Stand(evt.StandIndex);
            Vector2 slot = GetStandPosition(evt.StandIndex);
            if (!CanPlace(stand, slot) && TryFindFreeSpot(stand, slot, out Vector2 spot)) SetPosition(stand, spot);
        }

        private void OnLotStateChanged(LotStateChanged evt)
        {
            if (_buildings.GetLot(evt.LotIndex).state != LotState.Building) return;
            if (!BuildingManager.IsHouseLot(evt.LotIndex)) MakeRoomForMainBuilding();
            // Construction changes the occupied areas (terrain levelling, scenery, stand row).
            EventBus.Publish(new ObjectMoved(PlaceableKind.Lot, evt.LotIndex));
        }

        private void OnTierUpgraded(TierUpgraded evt)
        {
            if (evt.LotIndex != BuildingManager.MainLotIndex) return;
            Rect area = AreaForTier(evt.Tier);
            if (area != AreaForTier(evt.Tier - 1)) EventBus.Publish(new AreaUnlocked(evt.Tier, area));
        }

        /// <summary>
        /// The main building is turning into a bigger tier: if it no longer fits, it moves to the nearest free
        /// spot; anything it now covers (moved stands, decorations, houses) is moved out of the way; stands in
        /// the default row slide outward with it.
        /// </summary>
        private void MakeRoomForMainBuilding()
        {
            PlaceableId main = PlaceableId.Lot(BuildingManager.MainLotIndex);
            Vector2 at = GetLotPosition(main.Index);
            Rect footprint = GetFootprint(main, at);
            bool fitsLand = IsOnLand(footprint, false) && !PlacementRules.Overlaps(footprint, _database.world.relaxArea, 0f)
                && !PlacementRules.Overlaps(footprint, FootpathArea(), 0f);
            if (!fitsLand && TryFindFreeSpot(main, at, out Vector2 spot))
            {
                SetPosition(main, spot);
                footprint = GetFootprint(main, spot);
            }

            for (int i = 0; i < _buildings.LotCount; i++)
            {
                if (i != main.Index && IsPresent(PlaceableId.Lot(i))) MoveAwayFrom(footprint, PlaceableId.Lot(i));
            }

            for (int i = 0; i < _stands.Count; i++)
            {
                if (_stands[i].placed) MoveAwayFrom(footprint, PlaceableId.Stand(i));
            }

            for (int i = 0; i < _decorations.Count; i++) MoveAwayFrom(footprint, PlaceableId.Decoration(i));

            for (int i = 0; i < _stands.Count; i++)
            {
                PlaceableId stand = PlaceableId.Stand(i);
                Vector2 slot = GetStandPosition(i);
                if (!_stands[i].placed && !CanPlace(stand, slot) && TryFindFreeSpot(stand, slot, out Vector2 free))
                {
                    SetPosition(stand, free);
                }
            }
        }

        private void MoveAwayFrom(Rect footprint, PlaceableId id)
        {
            Vector2 at = GetPosition(id);
            Rect blocking = BlockingRect(id, GetFootprint(id, at), at);
            if (PlacementRules.Overlaps(footprint, blocking, _config.gap) && TryFindFreeSpot(id, at, out Vector2 spot))
            {
                SetPosition(id, spot);
            }
        }
    }
}
