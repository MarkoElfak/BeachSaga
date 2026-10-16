using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Single source of truth for where lots (main building, houses), stands and decorations stand, and how much
    /// ground each one covers (footprints measured from the prefabs). Every object moves on its own; moves are
    /// saved in the models. Rules live in PlacementService.Rules, growth and new stands in .Growth.
    /// </summary>
    public sealed partial class PlacementService : IDisposable
    {
        private static readonly Rect FallbackFootprint = new Rect(-1.5f, -1.5f, 3f, 3f);

        private readonly GameDatabase _database;
        private readonly BuildingManager _buildings;
        private readonly List<StandModel> _stands;
        private readonly List<DecorationModel> _decorations;
        private readonly PlacementConfig _config;
        private TerrainShape _terrain;

        public PlacementService(GameDatabase database, BuildingManager buildings, List<StandModel> stands,
            List<DecorationModel> decorations)
        {
            _database = database;
            _buildings = buildings;
            _stands = stands;
            _decorations = decorations;
            _config = database.placement;
            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
        }

        public PlacementConfig Config => _config;
        public int StandCount => _stands.Count;
        public int DecorationCount => _decorations.Count;

        /// <summary>Terrain is built by the Beach scene; until then decoration heights are not checked.</summary>
        public void AttachTerrain(TerrainShape terrain) => _terrain = terrain;

        public Vector2 GetPosition(PlaceableId id)
        {
            switch (id.Kind)
            {
                case PlaceableKind.Lot: return GetLotPosition(id.Index);
                case PlaceableKind.Stand: return GetStandPosition(id.Index);
                default: return GetDecorationPosition(id.Index);
            }
        }

        public Vector2 GetLotPosition(int lotIndex)
        {
            LotModel lot = _buildings.GetLot(lotIndex);
            if (lot.placed) return new Vector2(lot.x, lot.z);
            Vector2[] defaults = _database.world.lotPositions;
            return defaults != null && lotIndex < defaults.Length ? defaults[lotIndex] : Vector2.zero;
        }

        /// <summary>Unplaced stands sit in their default slot beside the main building (and follow it as it grows).</summary>
        public Vector2 GetStandPosition(int standIndex)
        {
            StandModel model = _stands[standIndex];
            return model.placed ? new Vector2(model.x, model.z) : DefaultStandPosition(standIndex);
        }

        public Vector2 GetDecorationPosition(int index)
        {
            DecorationModel model = _decorations[index];
            if (model.placed) return new Vector2(model.x, model.z);
            Vector2[] spots = _database.world.decorationSpots;
            return spots != null && model.spotIndex < spots.Length ? spots[model.spotIndex] : Vector2.zero;
        }

        /// <summary>The ground the object covers at 'at' (what visitors walk around; no queue space).</summary>
        public Rect GetFootprint(PlaceableId id, Vector2 at)
        {
            switch (id.Kind)
            {
                case PlaceableKind.Lot:
                    Rect lot = BuildingManager.IsHouseLot(id.Index) ? _database.world.houseFootprint : MainFootprint();
                    return PlacementRules.Offset(lot, at);
                case PlaceableKind.Stand:
                    return PlacementRules.Offset(StandFootprint(id.Index), at);
                default:
                    return PlacementRules.Offset(DecorationArea(_decorations[id.Index].decorationId), at);
            }
        }

        /// <summary>Footprint plus anything else the object keeps free (a stand's queue).</summary>
        public Rect GetBlockingArea(PlaceableId id, Vector2 at) => BlockingRect(id, GetFootprint(id, at), at);

        public bool CanPlace(PlaceableId id, Vector2 at)
        {
            Rect footprint = GetFootprint(id, at);
            return IsFree(footprint, BlockingRect(id, footprint, at), id);
        }

        public bool CanPlaceNewDecoration(DecorationDefinition definition, Vector2 at)
        {
            Rect footprint = PlacementRules.Offset(definition.area, at);
            return IsFree(footprint, footprint, PlaceableId.NewDecoration);
        }

        /// <summary>
        /// The player moved an object (call only after CanPlace). Moving the main building leaves its stands where
        /// they are: they are pinned to their current places first.
        /// </summary>
        public void Commit(PlaceableId id, Vector2 at)
        {
            if (id.IsLot && !BuildingManager.IsHouseLot(id.Index)) PinStands(id.Index);
            SetPosition(id, at);
        }

        /// <summary>Areas the terrain levels: every building and stand.</summary>
        public void GetFlatRects(List<Rect> into)
        {
            into.Clear();
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                if (IsPresent(PlaceableId.Lot(i))) into.Add(GetFootprint(PlaceableId.Lot(i), GetLotPosition(i)));
            }

            for (int i = 0; i < _stands.Count; i++) into.Add(GetFootprint(PlaceableId.Stand(i), GetStandPosition(i)));
        }

        /// <summary>Empty house lots are invisible reservations and do not block anything.</summary>
        public bool IsPresent(PlaceableId id)
        {
            switch (id.Kind)
            {
                case PlaceableKind.Lot:
                    return !BuildingManager.IsHouseLot(id.Index) || _buildings.GetLot(id.Index).state != LotState.Empty;
                case PlaceableKind.Stand:
                    return id.Index >= 0 && id.Index < _stands.Count;
                default:
                    return id.Index >= 0 && id.Index < _decorations.Count;
            }
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
        }

        // Writes the position into the save model and tells everyone (views, terrain, visitors).
        private void SetPosition(PlaceableId id, Vector2 at)
        {
            switch (id.Kind)
            {
                case PlaceableKind.Lot:
                    LotModel lot = _buildings.GetLot(id.Index);
                    lot.placed = true;
                    lot.x = at.x;
                    lot.z = at.y;
                    break;
                case PlaceableKind.Stand:
                    StandModel stand = _stands[id.Index];
                    stand.placed = true;
                    stand.x = at.x;
                    stand.z = at.y;
                    break;
                default:
                    DecorationModel decoration = _decorations[id.Index];
                    decoration.placed = true;
                    decoration.x = at.x;
                    decoration.z = at.y;
                    break;
            }

            EventBus.Publish(new ObjectMoved(id.Kind, id.Index));
        }
    }
}
