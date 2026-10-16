using BeachTycoon.Data;
using BeachTycoon.Services;
using NUnit.Framework;

namespace BeachTycoon.Tests
{
    public sealed class IslandSnapshotTests
    {
        private static IslandSnapshot Sample()
        {
            var snapshot = new IslandSnapshot { ownerId = "owner-1", ownerName = "Ana", tier = 2, popularity = 1.25f, timestampUtc = "2026-10-16T12:00:00.0000000Z" };
            snapshot.buildings.Add(new SnapshotBuilding { lotIndex = 0, tier = 2, x = 1.5f, z = -2f });
            snapshot.buildings.Add(new SnapshotBuilding { lotIndex = 3, tier = 1, variant = 2, x = -8f, z = -10f });
            snapshot.stands.Add(new SnapshotStand { productId = "donuts", level = 3, x = 4f, z = -1f, yaw = 90f });
            snapshot.decorations.Add(new SnapshotDecoration { decorationId = "umbrella", x = -3.25f, z = -6.5f, yaw = 45f });
            return snapshot;
        }

        [Test]
        public void RoundTrip_KeepsEveryField()
        {
            Assert.IsTrue(IslandSnapshotJson.TryParse(IslandSnapshotJson.ToJson(Sample()), out IslandSnapshot copy));
            Assert.AreEqual(IslandSnapshot.CurrentVersion, copy.version);
            Assert.AreEqual("owner-1", copy.ownerId);
            Assert.AreEqual("Ana", copy.ownerName);
            Assert.AreEqual(2, copy.tier);
            Assert.AreEqual(1.25f, copy.popularity);
            Assert.AreEqual("2026-10-16T12:00:00.0000000Z", copy.timestampUtc);
            Assert.AreEqual(2, copy.buildings.Count);
            Assert.AreEqual(3, copy.buildings[1].lotIndex);
            Assert.AreEqual(2, copy.buildings[1].variant);
            Assert.AreEqual(-8f, copy.buildings[1].x);
            Assert.AreEqual("donuts", copy.stands[0].productId);
            Assert.AreEqual(3, copy.stands[0].level);
            Assert.AreEqual(-6.5f, copy.decorations[0].z);
            Assert.AreEqual(90f, copy.stands[0].yaw);
            Assert.AreEqual(45f, copy.decorations[0].yaw);
        }

        [Test]
        public void BiggestAllowedIsland_StaysUnder50KB()
        {
            var snapshot = new IslandSnapshot { ownerId = "local-0123456789abcdef0123456789abcdef", ownerName = "A long player name here", tier = 5 };
            for (int i = 0; i < 32; i++) snapshot.buildings.Add(new SnapshotBuilding { lotIndex = i, tier = 5, variant = i, x = -123.456789f, z = 98.7654321f });
            for (int i = 0; i < 12; i++) snapshot.stands.Add(new SnapshotStand { productId = "product_number_" + i, level = 999, x = -123.456789f, z = 98.7654321f });
            for (int i = 0; i < IslandSnapshotValidator.MaxDecorations; i++)
            {
                snapshot.decorations.Add(new SnapshotDecoration { decorationId = "lifeguard_tower", x = -123.456789f, z = -98.7654321f });
            }

            int bytes = IslandSnapshotJson.ByteCount(IslandSnapshotJson.ToJson(snapshot));
            Assert.Less(bytes, IslandSnapshotJson.MaxBytes, $"{bytes} bytes");
        }

        [Test]
        public void TryParse_RejectsBadInput()
        {
            Assert.IsFalse(IslandSnapshotJson.TryParse(null, out _));
            Assert.IsFalse(IslandSnapshotJson.TryParse("", out _));
            Assert.IsFalse(IslandSnapshotJson.TryParse("not json", out _));
            Assert.IsFalse(IslandSnapshotJson.TryParse("{\"version\":99,\"ownerId\":\"x\"}", out _), "newer version");
            Assert.IsFalse(IslandSnapshotJson.TryParse("{\"version\":0,\"ownerId\":\"x\"}", out _), "no version");
            Assert.IsFalse(IslandSnapshotJson.TryParse("{\"ownerId\":\"" + new string('x', IslandSnapshotJson.MaxBytes) + "\"}", out _), "too big");
        }
    }
}
