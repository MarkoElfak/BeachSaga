using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>
    /// Placement rules on the default layout and map, with a two-tier test database (no terrain): the first tier
    /// opens the first beach, the second opens every later piece at once.
    /// </summary>
    public sealed partial class PlacementServiceTests
    {
        private const string DecorationId = "test_deco";
        private static readonly Rect TentFootprint = new Rect(-1.6f, -1.55f, 3.2f, 3.1f);
        private static readonly Rect ResortFootprint = new Rect(-2.2f, -2.6f, 4.4f, 5.1f);
        private static readonly Rect FirstArea = Rect.MinMaxRect(-11.8f, -15.6f, 11.8f, 6.5f); // tiers without a map
        private static readonly Rect LastArea = Rect.MinMaxRect(-19.2f, -16.9f, 19.2f, 6.5f);
        private static readonly Rect HomeBeach = Rect.MinMaxRect(-19.2f, -16.9f, 19.2f, 6.5f);
        private static readonly Rect WholeMainland = Rect.MinMaxRect(-28.8f, -20.8f, 28.8f, 6.5f);

        private readonly List<Object> _assets = new List<Object>();
        private GameDatabase _database;
        private BuildingManager _buildings;
        private List<StandModel> _stands;
        private List<DecorationModel> _decorations;
        private PlacementService _placement;
        private int _moves;
        private Rect _unlocked;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _database = Asset<GameDatabase>();
            _database.world = Asset<WorldLayoutConfig>();
            _database.terrain = Asset<TerrainConfig>();
            _database.placement = Asset<PlacementConfig>();
            _database.economy = Asset<EconomyConfig>();
            _database.visitors = Asset<VisitorConfig>();
            _database.map = Asset<MapConfig>();
            for (int i = 0; i < _database.map.pieces.Length; i++)
            {
                _database.map.pieces[i].unlockTier = Mathf.Min(_database.map.pieces[i].unlockTier, 2);
            }

            _database.tiers = new[] { Tier(TentFootprint, FirstArea), Tier(ResortFootprint, LastArea) };
            _database.products = new[] { Product("donuts"), Product("towels"), Product("surf_lessons") };
            var decoration = Asset<DecorationDefinition>();
            decoration.id = DecorationId;
            decoration.area = new Rect(-0.7f, -0.7f, 1.4f, 1.4f);
            _database.decorations = new[] { decoration };

            _buildings = new BuildingManager(_database, new EconomyService(new WalletModel(0)), new List<LotModel>());
            _stands = new List<StandModel>();
            _decorations = new List<DecorationModel>();
            _placement = new PlacementService(_database, _buildings, _stands, _decorations);
            _moves = 0;
            EventBus.Subscribe<ObjectMoved>(_ => _moves++);
            EventBus.Subscribe<AreaUnlocked>(evt => _unlocked = evt.Area);
            SetMain(LotState.Built, 1);
        }

        [TearDown]
        public void TearDown()
        {
            _placement.Dispose();
            EventBus.ClearAll();
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
            _assets.Clear();
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        private TierDefinition Tier(Rect footprint, Rect area)
        {
            var tier = Asset<TierDefinition>();
            tier.footprint = footprint;
            tier.buildArea = area;
            return tier;
        }

        private ProductDefinition Product(string id)
        {
            var product = Asset<ProductDefinition>();
            product.id = id;
            product.standFootprint = new Rect(-0.95f, -0.55f, 1.9f, 1.25f);
            product.queueStart = new Vector2(0f, -1.7f);
            return product;
        }

        private void SetMain(LotState state, int tier)
        {
            LotModel main = _buildings.GetLot(0);
            main.state = state;
            main.tier = state == LotState.Building ? tier - 1 : tier;
            main.targetTier = tier;
        }

        private void BuildAllHouses()
        {
            for (int i = 1; i < _buildings.LotCount; i++) _buildings.GetLot(i).state = LotState.Built;
        }

        private int AddStand(string productId)
        {
            _stands.Add(new StandModel { productId = productId, lotIndex = 0, slotIndex = _stands.Count });
            return _stands.Count - 1;
        }

        private int AddDecoration(int spot)
        {
            _decorations.Add(new DecorationModel { decorationId = DecorationId, spotIndex = spot });
            return _decorations.Count - 1;
        }

        [Test]
        public void DefaultLayout_EverythingFitsAtTheLastTier()
        {
            SetMain(LotState.Built, 2);
            BuildAllHouses();
            AddStand("donuts");
            AddStand("towels");
            for (int s = 0; s < _database.world.decorationSpots.Length; s++) AddDecoration(s);

            for (int i = 0; i < _buildings.LotCount; i++)
            {
                Assert.IsTrue(_placement.CanPlace(PlaceableId.Lot(i), _placement.GetLotPosition(i)), $"lot {i}");
            }

            for (int i = 0; i < _stands.Count; i++)
            {
                Assert.IsTrue(_placement.CanPlace(PlaceableId.Stand(i), _placement.GetStandPosition(i)), $"stand {i}");
            }

            for (int i = 0; i < _decorations.Count; i++)
            {
                Assert.IsTrue(_placement.CanPlace(PlaceableId.Decoration(i), _placement.GetDecorationPosition(i)), $"spot {i}");
            }
        }

        [Test]
        public void FirstTier_KeepsObjectsInsideTheUnlockedArea()
        {
            Assert.AreEqual(HomeBeach, _placement.UnlockedArea);
            int decoration = AddDecoration(0);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Decoration(decoration), new Vector2(15f, -2f)), "the whole first beach");
            Assert.IsFalse(_placement.CanPlace(PlaceableId.Decoration(decoration), new Vector2(-24f, -2f)), "locked land");
            SetMain(LotState.Built, 2);
            Assert.AreEqual(WholeMainland, _placement.UnlockedArea);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Decoration(decoration), new Vector2(-24f, -2f)), "unlocked by the upgrade");
        }

        [Test]
        public void WithoutAMap_TiersUseTheirOwnLand()
        {
            _database.map = null;
            Assert.AreEqual(FirstArea, _placement.UnlockedArea);
            Assert.AreEqual(FirstArea, _placement.UnlockedBounds);
            SetMain(LotState.Built, 2);
            Assert.AreEqual(LastArea, _placement.UnlockedArea);
        }

        [Test]
        public void Upgrade_PublishesTheNewAreaAndEachOpenedPiece()
        {
            var opened = new List<int>();
            EventBus.Subscribe<LandOpened>(evt => opened.Add(evt.Piece));
            SetMain(LotState.Built, 2);
            EventBus.Publish(new TierUpgraded(0, 2));
            Assert.AreEqual(Rect.MinMaxRect(-28.8f, -20.8f, 28.8f, 30.5f), _unlocked, "mainland and islet");
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, opened);
        }

        [Test]
        public void Lot_CannotOverlapAnotherBuilding()
        {
            BuildAllHouses();
            Assert.IsFalse(_placement.CanPlace(PlaceableId.Lot(1), _placement.GetLotPosition(2)));
        }

        [Test]
        public void Objects_StayOffTheFootpathTheTowelsAndTheSea()
        {
            PlaceableId id = PlaceableId.Decoration(AddDecoration(0));
            Assert.IsFalse(_placement.CanPlace(id, _database.terrain.path.center), "footpath");
            Assert.IsFalse(_placement.CanPlace(id, new Vector2(0f, -6f)), "where the footpath meets the beach");
            Assert.IsFalse(_placement.CanPlace(id, _database.world.relaxArea.center), "towel area");
            Assert.IsFalse(_placement.CanPlace(id, new Vector2(0f, 9f)), "surf");
        }

        [Test]
        public void Stand_KeepsItsQueueFree()
        {
            int stand = AddStand("donuts");
            Vector2 standAt = _placement.GetStandPosition(stand);
            PlaceableId decoration = PlaceableId.Decoration(AddDecoration(0));
            Assert.IsFalse(_placement.CanPlace(decoration, standAt + new Vector2(0f, -3.5f)), "in the queue");
            Rect blocking = _placement.GetBlockingArea(PlaceableId.Stand(stand), standAt);
            float queueEnd = standAt.y - 1.7f - QueueLayout.Length(_database.visitors);
            Assert.AreEqual(queueEnd, blocking.yMin, 1e-4f, "two short lines, not one long one");
            Assert.GreaterOrEqual(blocking.width, QueueLayout.Width(_database.visitors, _database.placement.queueWidth) - 1e-4f, "both lines fit");
        }

        [Test]
        public void DefaultStandRow_AlternatesRightAndLeftOfTheBuilding()
        {
            int right = AddStand("donuts");
            int left = AddStand("towels");
            Rect building = _placement.GetFootprint(PlaceableId.Lot(0), _placement.GetLotPosition(0));
            Assert.Greater(_placement.GetFootprint(PlaceableId.Stand(right), _placement.GetStandPosition(right)).xMin, building.xMax);
            Assert.Less(_placement.GetFootprint(PlaceableId.Stand(left), _placement.GetStandPosition(left)).xMax, building.xMin);
        }

        [Test]
        public void MovingTheMainBuilding_LeavesItsStandsWhereTheyAre()
        {
            int stand = AddStand("donuts");
            Vector2 before = _placement.GetStandPosition(stand);
            var target = new Vector2(-3f, -2f);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Lot(0), target));

            _placement.Commit(PlaceableId.Lot(0), target);

            Assert.AreEqual(target, _placement.GetLotPosition(0));
            Assert.AreEqual(before, _placement.GetStandPosition(stand));
            Assert.IsTrue(_stands[stand].placed);
        }

        [Test]
        public void GrowingBuilding_SlidesDefaultStandsOutward()
        {
            int stand = AddStand("donuts");
            float before = _placement.GetStandPosition(stand).x;
            SetMain(LotState.Building, 2);
            Assert.Greater(_placement.GetStandPosition(stand).x, before);
        }

        [Test]
        public void GrowingBuilding_PushesThingsOutOfTheWay()
        {
            int decoration = AddDecoration(0);
            var beside = new Vector2(-2.5f, -3f);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Decoration(decoration), beside), "fits next to the tent");
            _placement.Commit(PlaceableId.Decoration(decoration), beside);

            SetMain(LotState.Building, 2);
            EventBus.Publish(new LotStateChanged(0));

            Vector2 after = _placement.GetDecorationPosition(decoration);
            Assert.AreNotEqual(beside, after);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Decoration(decoration), after));
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
            SetMain(LotState.Built, 2);
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
        public void Footprints_ComeFromTheData()
        {
            int decoration = AddDecoration(3);
            Assert.AreEqual(new Rect(-0.7f, -0.7f, 1.4f, 1.4f), _placement.GetFootprint(PlaceableId.Decoration(decoration), Vector2.zero));
            Assert.AreEqual(TentFootprint, _placement.GetFootprint(PlaceableId.Lot(0), Vector2.zero));
            SetMain(LotState.Building, 2);
            Assert.AreEqual(ResortFootprint, _placement.GetFootprint(PlaceableId.Lot(0), Vector2.zero), "reserves the next tier");
        }
    }
}
