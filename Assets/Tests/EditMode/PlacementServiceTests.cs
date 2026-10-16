using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Placement rules against the default layout (config defaults, no terrain).</summary>
    public sealed class PlacementServiceTests
    {
        private const string DecorationId = "test_deco";

        private GameDatabase _database;
        private BuildingManager _buildings;
        private List<DecorationModel> _decorations;
        private PlacementService _placement;
        private int _moves;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _database = ScriptableObject.CreateInstance<GameDatabase>();
            _database.world = ScriptableObject.CreateInstance<WorldLayoutConfig>();
            _database.terrain = ScriptableObject.CreateInstance<TerrainConfig>();
            _database.placement = ScriptableObject.CreateInstance<PlacementConfig>();
            var decoration = ScriptableObject.CreateInstance<DecorationDefinition>();
            decoration.id = DecorationId;
            _database.decorations = new[] { decoration };

            _buildings = new BuildingManager(_database, new EconomyService(new WalletModel(0)), new List<LotModel>());
            _decorations = new List<DecorationModel>();
            _placement = new PlacementService(_database, _buildings, _decorations);
            _moves = 0;
            EventBus.Subscribe<ObjectMoved>(_ => _moves++);
        }

        [TearDown]
        public void TearDown()
        {
            _placement.Dispose();
            EventBus.ClearAll();
            Object.DestroyImmediate(_database.decorations[0]);
            Object.DestroyImmediate(_database.placement);
            Object.DestroyImmediate(_database.terrain);
            Object.DestroyImmediate(_database.world);
            Object.DestroyImmediate(_database);
        }

        private void BuildAllHouses()
        {
            for (int i = 0; i < _buildings.LotCount; i++) _buildings.GetLot(i).state = LotState.Built;
        }

        private int AddDecoration(int spot)
        {
            _decorations.Add(new DecorationModel { decorationId = DecorationId, spotIndex = spot });
            return _decorations.Count - 1;
        }

        [Test]
        public void DefaultLayout_EveryLotAndSpotIsValidWhenAllAreTaken()
        {
            BuildAllHouses();
            for (int s = 0; s < _database.world.decorationSpots.Length; s++) AddDecoration(s);

            for (int i = 0; i < _buildings.LotCount; i++)
            {
                Assert.IsTrue(_placement.CanPlace(PlaceableId.Lot(i), _placement.GetLotPosition(i)), $"lot {i}");
            }

            for (int i = 0; i < _decorations.Count; i++)
            {
                Assert.IsTrue(_placement.CanPlace(PlaceableId.Decoration(i), _placement.GetDecorationPosition(i)),
                    $"decoration spot {i}");
            }
        }

        [Test]
        public void DefaultSpots_DoNotBlockEmptyHouseLots()
        {
            for (int s = 0; s < _database.world.decorationSpots.Length; s++)
            {
                Assert.IsTrue(_placement.CanPlaceNewDecoration(_database.decorations[0], _database.world.decorationSpots[s]),
                    $"spot {s}");
            }
        }

        [Test]
        public void Lot_CannotOverlapAnotherBuilding()
        {
            BuildAllHouses();
            Vector2 neighbour = _placement.GetLotPosition(2);
            Assert.IsFalse(_placement.CanPlace(PlaceableId.Lot(1), neighbour));
        }

        [Test]
        public void Objects_StayOffThePathTheTowelsAndTheSea()
        {
            int decoration = AddDecoration(0);
            PlaceableId id = PlaceableId.Decoration(decoration);
            Assert.IsFalse(_placement.CanPlace(id, _database.terrain.path.center), "footpath");
            Assert.IsFalse(_placement.CanPlace(id, _database.world.relaxArea.center), "towel area");
            Assert.IsFalse(_placement.CanPlace(id, new Vector2(0f, 9f)), "surf (outside the build area)");
            Assert.IsFalse(_placement.CanPlace(id, new Vector2(30f, 0f)), "off the island");
        }

        [Test]
        public void Decoration_CannotBlockTheQueuesInFrontOfTheMainBuilding()
        {
            int decoration = AddDecoration(0);
            Vector2 main = _placement.GetLotPosition(BuildingManager.MainLotIndex);
            Rect footprint = _placement.GetFootprint(PlaceableId.Lot(0), main);
            var inQueueSpace = new Vector2(footprint.center.x + 3f, footprint.yMin - 1.5f);
            Assert.IsFalse(_placement.CanPlace(PlaceableId.Decoration(decoration), inQueueSpace));
        }

        [Test]
        public void Commit_SavesPositionInTheModelAndPublishes()
        {
            int decoration = AddDecoration(0);
            var target = new Vector2(-10f, -4f);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Decoration(decoration), target));

            _placement.Commit(PlaceableId.Decoration(decoration), target);

            Assert.IsTrue(_decorations[decoration].placed);
            Assert.AreEqual(target, _placement.GetDecorationPosition(decoration));
            Assert.AreEqual(1, _moves);
        }

        [Test]
        public void EnsureLotIsFree_RelocatesAHouseWhenSomethingWasMovedOntoItsSpot()
        {
            const int lot = 1;
            Vector2 reserved = _placement.GetLotPosition(lot);
            int decoration = AddDecoration(0);
            _placement.Commit(PlaceableId.Decoration(decoration), reserved); // empty lots do not block moves

            _buildings.GetLot(lot).state = LotState.Building;
            Assert.IsTrue(_placement.EnsureLotIsFree(lot));

            Vector2 moved = _placement.GetLotPosition(lot);
            Assert.AreNotEqual(reserved, moved);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Lot(lot), moved));
        }

        [Test]
        public void EnsureLotIsFree_KeepsTheDefaultSpotWhenItIsFree()
        {
            BuildAllHouses();
            for (int i = 1; i < _buildings.LotCount; i++)
            {
                Vector2 before = _placement.GetLotPosition(i);
                Assert.IsTrue(_placement.EnsureLotIsFree(i));
                Assert.AreEqual(before, _placement.GetLotPosition(i), $"lot {i}");
                Assert.IsFalse(_buildings.GetLot(i).placed);
            }
        }

        [Test]
        public void FootprintIndexes_MatchTheirIds()
        {
            int decoration = AddDecoration(3);
            Rect rect = _placement.GetFootprint(PlaceableId.Decoration(decoration), Vector2.zero);
            Assert.AreEqual(_database.decorations[0].footprint, rect.size);
            Assert.AreEqual(Vector2.zero, rect.center);
        }
    }
}
