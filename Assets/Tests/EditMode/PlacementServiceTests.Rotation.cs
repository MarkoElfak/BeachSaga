using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Turning objects in place (PlacementService.Rotation).</summary>
    public sealed partial class PlacementServiceTests
    {
        private PlaceableId OpenDecoration()
        {
            PlaceableId id = PlaceableId.Decoration(AddDecoration(0));
            var open = new Vector2(-10f, -4f);
            Assert.IsTrue(_placement.CanPlace(id, open), "open ground");
            _placement.Commit(id, open);
            _moves = 0;
            return id;
        }

        [Test]
        public void Rotate_TurnsOneStepSavesAndPublishes()
        {
            PlaceableId id = OpenDecoration();
            Assert.AreEqual(45f, _placement.RotationStepDegrees, "8 steps by default");

            Assert.IsTrue(_placement.TryRotate(id, 1));
            Assert.AreEqual(45f, _decorations[id.Index].yaw);
            Assert.AreEqual(1, _moves);
            Assert.AreEqual(45f, _placement.GetRotation(id).eulerAngles.y, 1e-3f);

            Assert.IsTrue(_placement.TryRotate(id, -1));
            Assert.IsTrue(_placement.TryRotate(id, -1));
            Assert.AreEqual(315f, _decorations[id.Index].yaw, "turning left past 0 wraps around");
        }

        [Test]
        public void Rotate_StepsComeFromTheConfig()
        {
            _database.placement.rotationSteps = 4;
            PlaceableId id = OpenDecoration();
            for (int i = 0; i < 4; i++) Assert.IsTrue(_placement.TryRotate(id, 1), $"step {i}");
            Assert.AreEqual(0f, _decorations[id.Index].yaw, "a full circle is back to the start");
            _placement.TryRotate(id, 1);
            Assert.AreEqual(90f, _decorations[id.Index].yaw);
        }

        [Test]
        public void Rotate_TurnsTheStandsQueueToo()
        {
            int stand = AddStand("donuts");
            PlaceableId id = PlaceableId.Stand(stand);
            Vector2 at = _placement.GetStandPosition(stand);
            Rect before = _placement.GetBlockingArea(id, at);
            _stands[stand].yaw = 90f;
            Rect after = _placement.GetBlockingArea(id, at);
            Assert.AreEqual(before.height, after.width, 1e-4f, "the queue strip now runs sideways");
            Assert.Less(after.xMin, at.x - 3f, "towards -x after a clockwise quarter turn");
        }

        [Test]
        public void Rotate_SkipsDirectionsThatDoNotFit()
        {
            int stand = AddStand("donuts");
            PlaceableId id = PlaceableId.Stand(stand);
            Vector2 at = _placement.GetStandPosition(stand);
            PlaceableId neighbour = PlaceableId.Decoration(AddDecoration(0));
            Vector2 beside = at + new Vector2(2.5f, -1.5f);
            Assert.IsTrue(_placement.CanPlace(neighbour, beside), "fits beside the unturned stand");
            _placement.Commit(neighbour, beside);
            _stands[stand].yaw = 315f;
            Assert.IsFalse(_placement.CanPlace(id, at), "one step left the queue would hit the neighbour");
            _stands[stand].yaw = 0f;

            Assert.IsTrue(_placement.TryRotate(id, -1));
            float yaw = _stands[stand].yaw;
            Assert.AreNotEqual(315f, yaw, "the blocked step was skipped");
            Assert.AreNotEqual(0f, yaw);
            Assert.IsTrue(_placement.CanPlace(id, at), $"fits at {yaw}");
        }

        [Test]
        public void Rotate_IsRefusedWhenNoOtherDirectionFits()
        {
            _database.placement.rotationSteps = 2; // facing front or back only
            int stand = AddStand("donuts");
            PlaceableId id = PlaceableId.Stand(stand);
            Vector2 at = _placement.GetStandPosition(stand);
            PlaceableId behind = PlaceableId.Decoration(AddDecoration(0));
            Assert.IsTrue(_placement.CanPlace(behind, at + new Vector2(0f, 3f)), "fits behind the stand");
            _placement.Commit(behind, at + new Vector2(0f, 3f));
            _moves = 0;

            Assert.IsFalse(_placement.CanRotate(id, 1));
            Assert.IsFalse(_placement.TryRotate(id, 1));
            Assert.AreEqual(0f, _stands[stand].yaw);
            Assert.AreEqual(0, _moves);
        }

        [Test]
        public void Rotate_HandlesNonSquareFootprints()
        {
            _buildings.GetLot(0).yaw = 90f;
            Rect turned = _placement.GetFootprint(PlaceableId.Lot(0), Vector2.zero);
            Assert.AreEqual(TentFootprint.height, turned.width, 1e-4f);
            Assert.AreEqual(TentFootprint.width, turned.height, 1e-4f);
        }
    }
}
