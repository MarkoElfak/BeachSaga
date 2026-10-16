using BeachTycoon.Data;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class IslandSnapshotValidatorTests
    {
        private OnlineTestData _data;

        [SetUp]
        public void SetUp() => _data = new OnlineTestData();

        [TearDown]
        public void TearDown() => _data.Dispose();

        [Test]
        public void UnusableSnapshots_AreRejected()
        {
            Assert.IsNull(IslandSnapshotValidator.Sanitize(null, _data.Database));
            Assert.IsNull(IslandSnapshotValidator.Sanitize(new IslandSnapshot(), _data.Database), "no owner");
            Assert.IsNull(IslandSnapshotValidator.Sanitize(new IslandSnapshot { ownerId = "a", version = 2 }, _data.Database), "newer version");
        }

        [Test]
        public void UnknownBrokenAndDuplicateEntries_AreDropped()
        {
            var raw = new IslandSnapshot { ownerId = "a", ownerName = "  A name that is far too long for the banner  ", tier = 1, popularity = float.NaN };
            raw.buildings.Add(new SnapshotBuilding { lotIndex = 0, tier = 99, x = 1f, z = 1f });
            raw.buildings.Add(new SnapshotBuilding { lotIndex = 0, tier = 1 });
            raw.buildings.Add(new SnapshotBuilding { lotIndex = 2, tier = 7, x = float.PositiveInfinity });
            raw.buildings.Add(new SnapshotBuilding { lotIndex = 3, tier = 7, variant = -4 });
            raw.buildings.Add(new SnapshotBuilding { lotIndex = 500 });
            raw.stands.Add(new SnapshotStand { productId = "donuts", level = -2 });
            raw.stands.Add(new SnapshotStand { productId = "donuts" });
            raw.stands.Add(new SnapshotStand { productId = "rockets" });
            raw.stands.Add(new SnapshotStand { productId = "towels", x = float.NaN });
            raw.decorations.Add(new SnapshotDecoration { decorationId = "umbrella", x = 2f });
            raw.decorations.Add(new SnapshotDecoration { decorationId = "dragon" });
            raw.decorations.Add(new SnapshotDecoration { decorationId = "umbrella", z = 9999f });

            IslandSnapshot clean = IslandSnapshotValidator.Sanitize(raw, _data.Database);

            Assert.AreEqual(IslandSnapshotValidator.MaxNameLength, clean.ownerName.Length);
            Assert.AreEqual(0f, clean.popularity);
            Assert.AreEqual(2, clean.buildings.Count, "first main building and house 3");
            Assert.AreEqual(3, clean.buildings[0].tier, "main tier clamped to the last tier");
            Assert.AreEqual(3, clean.tier, "snapshot tier follows the main building");
            Assert.AreEqual(1, clean.buildings[1].tier, "houses are always tier 1");
            Assert.AreEqual(0, clean.buildings[1].variant);
            Assert.AreEqual(1, clean.stands.Count);
            Assert.AreEqual(0, clean.stands[0].level);
            Assert.AreEqual(1, clean.decorations.Count);
            Assert.AreEqual(2f, clean.decorations[0].x);
        }

        [Test]
        public void Decorations_AreCapped()
        {
            var raw = new IslandSnapshot { ownerId = "a" };
            for (int i = 0; i < IslandSnapshotValidator.MaxDecorations + 50; i++) raw.decorations.Add(new SnapshotDecoration { decorationId = "umbrella" });
            Assert.AreEqual(IslandSnapshotValidator.MaxDecorations, IslandSnapshotValidator.Sanitize(raw, _data.Database).decorations.Count);
        }

        [Test]
        public void Turns_AreKeptWithinAFullCircle()
        {
            var raw = new IslandSnapshot { ownerId = "a" };
            raw.buildings.Add(new SnapshotBuilding { lotIndex = 0, tier = 1, yaw = 450f });
            raw.stands.Add(new SnapshotStand { productId = "donuts", yaw = float.NaN });
            raw.decorations.Add(new SnapshotDecoration { decorationId = "umbrella", yaw = -45f });

            IslandSnapshot clean = IslandSnapshotValidator.Sanitize(raw, _data.Database);

            Assert.AreEqual(90f, clean.buildings[0].yaw);
            Assert.AreEqual(0f, clean.stands[0].yaw, "broken turn faces the default way");
            Assert.AreEqual(315f, clean.decorations[0].yaw);
        }
    }
}
