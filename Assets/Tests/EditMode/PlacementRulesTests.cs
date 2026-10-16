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

        [Test]
        public void Rotate_QuarterTurnSwapsTheSides()
        {
            var local = new Rect(-1.6f, -1.55f, 3.2f, 3.1f);
            Assert.AreEqual(local, PlacementRules.Rotate(local, 0f), "no turn");
            Assert.AreEqual(local, PlacementRules.Rotate(local, 360f), "full circle");
            AssertRect(Rect.MinMaxRect(-1.55f, -1.6f, 1.55f, 1.6f), PlacementRules.Rotate(local, 90f));
        }

        [Test]
        public void Rotate_FollowsUnityYaw()
        {
            // A strip in front of the object (towards -z) ends up on its -x side after a clockwise quarter turn,
            // just like Quaternion.Euler(0, 90, 0) turns (0, 0, -1) into (-1, 0, 0).
            Rect turned = PlacementRules.Rotate(Rect.MinMaxRect(-0.5f, -3f, 0.5f, 0f), 90f);
            AssertRect(Rect.MinMaxRect(-3f, -0.5f, 0f, 0.5f), turned);
            Vector3 unity = Quaternion.Euler(0f, 90f, 0f) * new Vector3(0f, 0f, -3f);
            Assert.AreEqual(unity.x, turned.xMin, 1e-4f);
        }

        [Test]
        public void Rotate_DiagonalTurnUsesTheBoundingBox()
        {
            Rect turned = PlacementRules.Rotate(new Rect(-1f, -1f, 2f, 2f), 45f);
            AssertRect(Rect.MinMaxRect(-Mathf.Sqrt(2f), -Mathf.Sqrt(2f), Mathf.Sqrt(2f), Mathf.Sqrt(2f)), turned);
        }

        private static void AssertRect(Rect expected, Rect actual)
        {
            Assert.AreEqual(expected.xMin, actual.xMin, 1e-4f, "xMin");
            Assert.AreEqual(expected.xMax, actual.xMax, 1e-4f, "xMax");
            Assert.AreEqual(expected.yMin, actual.yMin, 1e-4f, "zMin");
            Assert.AreEqual(expected.yMax, actual.yMax, 1e-4f, "zMax");
        }
    }
}
