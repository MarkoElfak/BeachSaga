using System.Collections.Generic;
using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    public sealed class PlacementRulesTests
    {
        [Test]
        public void Snap_RoundsToNearestGridPoint()
        {
            Assert.AreEqual(new Vector2(0.5f, 0.5f), PlacementRules.Snap(new Vector2(0.26f, 0.74f), 0.5f));
            Assert.AreEqual(new Vector2(-1f, 2f), PlacementRules.Snap(new Vector2(-1.2f, 2.2f), 0.5f));
            Assert.AreEqual(new Vector2(0.3f, 0.7f), PlacementRules.Snap(new Vector2(0.3f, 0.7f), 0f), "no grid");
        }

        [Test]
        public void Overlaps_RespectsGap()
        {
            var a = new Rect(0f, 0f, 1f, 1f);
            Assert.IsTrue(PlacementRules.Overlaps(a, new Rect(0.5f, 0.5f, 1f, 1f), 0f), "intersecting");
            Assert.IsFalse(PlacementRules.Overlaps(a, new Rect(1f, 0f, 1f, 1f), 0f), "touching edges is fine");
            Assert.IsTrue(PlacementRules.Overlaps(a, new Rect(1.125f, 0f, 1f, 1f), 0.25f), "closer than the gap");
            Assert.IsFalse(PlacementRules.Overlaps(a, new Rect(1.25f, 0f, 1f, 1f), 0.25f), "exactly the gap");
            Assert.IsFalse(PlacementRules.Overlaps(a, new Rect(0f, 3f, 1f, 1f), 0.25f), "far away on z");
        }

        [Test]
        public void Inside_HonoursMargin()
        {
            var outer = new Rect(0f, 0f, 10f, 10f);
            Assert.IsTrue(PlacementRules.Inside(new Rect(1f, 1f, 2f, 2f), outer, 1f));
            Assert.IsFalse(PlacementRules.Inside(new Rect(0.5f, 1f, 2f, 2f), outer, 1f));
            Assert.IsFalse(PlacementRules.Inside(new Rect(8f, 8f, 2f, 2f), outer, 0.5f));
        }

        [Test]
        public void CenteredAndOffset_BuildExpectedRects()
        {
            Assert.AreEqual(new Rect(1f, 2f, 2f, 4f), PlacementRules.Centered(new Vector2(2f, 4f), new Vector2(2f, 4f)));
            Assert.AreEqual(new Rect(4f, -1f, 2f, 2f), PlacementRules.Offset(new Rect(-1f, -1f, 2f, 2f), new Vector2(5f, 0f)));
        }

        [Test]
        public void Spiral_StartsAtCentreAndVisitsEveryCellOnce()
        {
            const float step = 0.5f;
            const float radius = 3f;
            var center = new Vector2(1f, -2f);
            Assert.AreEqual(center, PlacementRules.Spiral(center, step, 0));

            int count = PlacementRules.SpiralCount(radius, step);
            int rings = Mathf.CeilToInt(radius / step);
            Assert.AreEqual((2 * rings + 1) * (2 * rings + 1), count);

            var seen = new HashSet<Vector2Int>();
            int lastRing = 0;
            for (int n = 0; n < count; n++)
            {
                Vector2 offset = (PlacementRules.Spiral(center, step, n) - center) / step;
                var cell = new Vector2Int(Mathf.RoundToInt(offset.x), Mathf.RoundToInt(offset.y));
                int ring = Mathf.Max(Mathf.Abs(cell.x), Mathf.Abs(cell.y));
                Assert.IsTrue(seen.Add(cell), $"cell {cell} visited twice (n = {n})");
                Assert.GreaterOrEqual(ring, lastRing, "the search must walk outwards");
                Assert.LessOrEqual(ring, rings);
                lastRing = ring;
            }
        }
    }
}
