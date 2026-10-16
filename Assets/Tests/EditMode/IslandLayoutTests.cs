using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Capturing an island, rebuilding it read-only from the snapshot, and the generated demo islands.</summary>
    public sealed class IslandLayoutTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
        private OnlineTestData _data;

        [SetUp]
        public void SetUp()
        {
            EventBus.ClearAll();
            _data = new OnlineTestData();
        }

        [TearDown]
        public void TearDown()
        {
            _data.Dispose();
            EventBus.ClearAll();
        }

        [Test]
        public void Capture_ThenRebuild_KeepsEveryObjectWhereItWas()
        {
            var home = new IslandLayout(_data.Database, new List<LotModel>(), new List<StandModel>(), new List<DecorationModel>());
            LotModel main = home.Buildings.GetLot(0);
            main.state = LotState.Built;
            main.tier = 2;
            LotModel house = home.Buildings.GetLot(4);
            house.state = LotState.Built;
            house.tier = 1;
            house.variant = 3;
            home.Buildings.GetLot(5).state = LotState.Building; // not finished: not in the snapshot
            home.Stands.Add(new StandModel { productId = "donuts", slotIndex = 0, level = 2 }); // default slot
            home.Stands.Add(new StandModel { productId = "towels", slotIndex = 1, placed = true, x = -6f, z = -4f });
            home.Decorations.Add(new DecorationModel { decorationId = "umbrella", spotIndex = 1 });

            IslandSnapshot snapshot = IslandSnapshotFactory.Capture("me", "Me", home.Lots, home.Stands, home.Decorations,
                home.Placement, 0.8f, Now);
            Assert.AreEqual(2, snapshot.tier);
            Assert.AreEqual(2, snapshot.buildings.Count, "main building and the finished house");
            Assert.AreEqual(0.8f, snapshot.popularity);
            Assert.AreEqual(Now, DateTime.Parse(snapshot.timestampUtc).ToUniversalTime());

            IslandSnapshotJson.TryParse(IslandSnapshotJson.ToJson(snapshot), out IslandSnapshot loaded);
            var visit = IslandLayout.FromSnapshot(_data.Database, IslandSnapshotValidator.Sanitize(loaded, _data.Database));
            Assert.AreEqual(home.Buildings.LotCount, visit.Buildings.LotCount);
            Assert.AreEqual(2, visit.Buildings.MainTier);
            Assert.AreEqual(LotState.Built, visit.Buildings.GetLot(4).state);
            Assert.AreEqual(3, visit.Buildings.GetLot(4).variant);
            Assert.AreEqual(LotState.Empty, visit.Buildings.GetLot(5).state);
            Assert.AreEqual(home.Placement.GetLotPosition(4), visit.Placement.GetLotPosition(4));
            Assert.AreEqual(home.Placement.GetStandPosition(0), visit.Placement.GetStandPosition(0), "default slot becomes an exact position");
            Assert.AreEqual(new Vector2(-6f, -4f), visit.Placement.GetStandPosition(1));
            Assert.AreEqual(2, visit.Stands[0].level);
            Assert.AreEqual(home.Placement.GetDecorationPosition(0), visit.Placement.GetDecorationPosition(0));
            home.Dispose();
            visit.Dispose();
        }

        [Test]
        public void ReadOnlyLayout_IgnoresTheHomeIslandsEvents()
        {
            var snapshot = new IslandSnapshot { ownerId = "a", tier = 1 };
            snapshot.buildings.Add(new SnapshotBuilding { lotIndex = 0, tier = 1, x = 2f, z = -3f });
            var visit = IslandLayout.FromSnapshot(_data.Database, snapshot);
            int moves = 0;
            EventBus.Subscribe<ObjectMoved>(_ => moves++);
            EventBus.Publish(new StandCreated(7, 0)); // a home stand index the visit does not have
            EventBus.Publish(new LotStateChanged(0));
            Assert.AreEqual(0, moves);
            Assert.AreEqual(new Vector2(2f, -3f), visit.Placement.GetLotPosition(0));
            visit.Dispose();
        }

        [Test]
        public void DemoIsland_HasTheTiersContentWithNothingOverlapping()
        {
            var demo = new DemoIsland { ownerId = "demo", displayName = "Demo", tier = 3, houses = 4, decorations = 5, popularity = 1f };
            IslandSnapshot snapshot = DemoIslands.Create(_data.Database, demo, Now);

            Assert.AreEqual(3, snapshot.tier);
            Assert.AreEqual("Demo", snapshot.ownerName);
            Assert.AreEqual(5, snapshot.buildings.Count, "main building and four houses");
            Assert.AreEqual(3, snapshot.stands.Count, "every product the tier allows");
            Assert.AreEqual(5, snapshot.decorations.Count);
            Assert.Less(IslandSnapshotJson.ByteCount(IslandSnapshotJson.ToJson(snapshot)), IslandSnapshotJson.MaxBytes);

            var layout = IslandLayout.FromSnapshot(_data.Database, IslandSnapshotValidator.Sanitize(snapshot, _data.Database));
            for (int i = 0; i < layout.Buildings.LotCount; i++)
            {
                if (layout.Placement.IsPresent(PlaceableId.Lot(i))) Assert.IsTrue(layout.Placement.CanPlace(PlaceableId.Lot(i), layout.Placement.GetLotPosition(i)), $"lot {i}");
            }

            for (int i = 0; i < layout.Stands.Count; i++) Assert.IsTrue(layout.Placement.CanPlace(PlaceableId.Stand(i), layout.Placement.GetStandPosition(i)), $"stand {i}");
            for (int i = 0; i < layout.Decorations.Count; i++) Assert.IsTrue(layout.Placement.CanPlace(PlaceableId.Decoration(i), layout.Placement.GetDecorationPosition(i)), $"decoration {i}");
            layout.Dispose();
        }

        [Test]
        public void DemoFriends_ComeFromTheConfig()
        {
            List<FriendInfo> friends = DemoIslands.Friends(_data.Database.online);
            Assert.AreEqual(_data.Database.online.demoFriends.Length, friends.Count);
            Assert.AreEqual(_data.Database.online.demoFriends[0].ownerId, friends[0].OwnerId);
        }
    }
}
