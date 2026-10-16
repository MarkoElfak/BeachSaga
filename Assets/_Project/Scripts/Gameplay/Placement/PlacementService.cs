using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Single source of truth for where lots and decorations stand. A position is valid when the footprint is
    /// inside the build area (decorations also above the water), off the footpath and towel area, and clear of
    /// every other object (plus the queue space in front of the main building). Moves are saved in the models.
    /// </summary>
    public sealed partial class PlacementService : IDisposable
    {
        private readonly GameDatabase _database;
        private readonly BuildingManager _buildings;
        private readonly List<DecorationModel> _decorations;
        private readonly PlacementConfig _config;
        private TerrainShape _terrain;

        public PlacementService(GameDatabase database, BuildingManager buildings, List<DecorationModel> decorations)
        {
            _database = database;
            _buildings = buildings;
            _decorations = decorations;
            _config = database.placement;
            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
        }

        public PlacementConfig Config => _config;

        /// <summary>Terrain is built by the Beach scene; until then decoration heights are not checked.</summary>
        public void AttachTerrain(TerrainShape terrain) => _terrain = terrain;

        public Vector2 GetPosition(PlaceableId id) => id.IsLot ? GetLotPosition(id.Index) : GetDecorationPosition(id.Index);

        public Vector2 GetLotPosition(int lotIndex)
        {
            LotModel lot = _buildings.GetLot(lotIndex);
            if (lot.placed) return new Vector2(lot.x, lot.z);
            Vector2[] defaults = _database.world.lotPositions;
            return defaults != null && lotIndex < defaults.Length ? defaults[lotIndex] : Vector2.zero;
        }

        public Vector2 GetDecorationPosition(int index)
        {
            DecorationModel model = _decorations[index];
            if (model.placed) return new Vector2(model.x, model.z);
            Vector2[] spots = _database.world.decorationSpots;
            return spots != null && model.spotIndex < spots.Length ? spots[model.spotIndex] : Vector2.zero;
        }

        /// <summary>The area the object would occupy at 'at' (what visitors walk around, without queue space).</summary>
        public Rect GetFootprint(PlaceableId id, Vector2 at)
        {
            if (id.IsLot)
            {
                WorldLayoutConfig layout = _database.world;
                Rect relative = BuildingManager.IsHouseLot(id.Index) ? layout.houseFootprint : layout.mainFootprint;
                return PlacementRules.Offset(relative, at);
            }

            return PlacementRules.Centered(at, DecorationSize(_decorations[id.Index].decorationId));
        }

        public bool CanPlace(PlaceableId id, Vector2 at)
        {
            Rect footprint = GetFootprint(id, at);
            return IsFree(footprint, BlockingRect(id, footprint), id);
        }

        public bool CanPlaceNewDecoration(DecorationDefinition definition, Vector2 at)
        {
            Rect footprint = PlacementRules.Centered(at, definition.footprint);
            return IsFree(footprint, footprint, PlaceableId.NewDecoration);
        }

        /// <summary>Moves the object for good (call only after CanPlace) and tells everyone.</summary>
        public void Commit(PlaceableId id, Vector2 at)
        {
            if (id.IsLot)
            {
                LotModel lot = _buildings.GetLot(id.Index);
                lot.placed = true;
                lot.x = at.x;
                lot.z = at.y;
            }
            else
            {
                DecorationModel model = _decorations[id.Index];
                model.placed = true;
                model.x = at.x;
                model.z = at.y;
            }

            EventBus.Publish(new ObjectMoved(id.IsLot, id.Index));
        }

        /// <summary>Areas the terrain flattens: the main lot and every occupied house lot.</summary>
        public void GetFlatRects(List<Rect> into)
        {
            into.Clear();
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                if (IsPresent(PlaceableId.Lot(i))) into.Add(GetFootprint(PlaceableId.Lot(i), GetLotPosition(i)));
            }
        }

        /// <summary>Areas taken by objects and the main queue space (scenery there is hidden).</summary>
        public void GetOccupiedRects(List<Rect> into)
        {
            into.Clear();
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                PlaceableId lot = PlaceableId.Lot(i);
                if (IsPresent(lot)) into.Add(BlockingRect(lot, GetFootprint(lot, GetLotPosition(i))));
            }

            for (int i = 0; i < _decorations.Count; i++)
            {
                into.Add(GetFootprint(PlaceableId.Decoration(i), GetDecorationPosition(i)));
            }
        }

        /// <summary>Empty house lots are invisible reservations and do not block anything.</summary>
        public bool IsPresent(PlaceableId id)
        {
            if (!id.IsLot) return id.Index >= 0 && id.Index < _decorations.Count;
            return !BuildingManager.IsHouseLot(id.Index) || _buildings.GetLot(id.Index).state != LotState.Empty;
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
        }

        // A house starting construction changes the occupied areas (terrain flattening, scenery).
        private void OnLotStateChanged(LotStateChanged evt)
        {
            if (BuildingManager.IsHouseLot(evt.LotIndex) && _buildings.GetLot(evt.LotIndex).state == LotState.Building)
            {
                EventBus.Publish(new ObjectMoved(true, evt.LotIndex));
            }
        }

        // The main building also reserves the queue space in front of its stands.
        private Rect BlockingRect(PlaceableId id, Rect footprint)
        {
            if (!id.IsLot || BuildingManager.IsHouseLot(id.Index)) return footprint;
            float extra = _config.mainFrontClearance;
            return new Rect(footprint.x, footprint.y - extra, footprint.width, footprint.height + extra);
        }

        // 'footprint' must stay on dry land, off the footpath and the towel area; 'blocking' (footprint plus any
        // reserved queue space) must stay clear of every other object.
        private bool IsFree(Rect footprint, Rect blocking, PlaceableId self)
        {
            if (!IsOnLand(footprint, !self.IsLot)) return false;

            WorldLayoutConfig layout = _database.world;
            if (PlacementRules.Overlaps(footprint, layout.relaxArea, 0f)) return false;
            if (_database.terrain != null && PlacementRules.Overlaps(footprint, _database.terrain.path, 0f)) return false;

            Rect rect = blocking;
            bool avoidReserved = self.Equals(PlaceableId.NewDecoration); // new decorations keep off empty house lots
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                PlaceableId other = PlaceableId.Lot(i);
                if (other.Equals(self) || (!avoidReserved && !IsPresent(other))) continue;
                Rect otherRect = BlockingRect(other, GetFootprint(other, GetLotPosition(i)));
                if (PlacementRules.Overlaps(rect, otherRect, _config.gap)) return false;
            }

            for (int i = 0; i < _decorations.Count; i++)
            {
                PlaceableId other = PlaceableId.Decoration(i);
                if (other.Equals(self)) continue;
                if (PlacementRules.Overlaps(rect, GetFootprint(other, GetDecorationPosition(i)), _config.gap)) return false;
            }

            return true;
        }

        // Buildings level the ground under them, so only decorations check the natural height.
        private bool IsOnLand(Rect rect, bool checkHeight)
        {
            if (!PlacementRules.Inside(rect, _config.buildArea, 0f)) return false;
            if (!checkHeight || _terrain == null) return true; // no terrain yet (boot, tests)
            return _terrain.Height(rect.center.x, rect.center.y, false) >= _terrain.WaterLevel + _config.minHeightAboveWater;
        }

        private Vector2 DecorationSize(string decorationId)
        {
            DecorationDefinition definition = _database.FindDecoration(decorationId);
            return definition != null ? definition.footprint : Vector2.one;
        }
    }
}
