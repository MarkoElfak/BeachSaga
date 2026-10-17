using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Shore-only stands (the surf school) stand at the water.</summary>
    public sealed partial class PlacementServiceTests
    {
        private const string Surf = "surf_lessons";
        private static readonly Vector2 Inland = new Vector2(-10f, -4f);

        private bool AtShore(int stand)
        {
            Rect footprint = _placement.GetFootprint(PlaceableId.Stand(stand), _placement.GetStandPosition(stand));
            return footprint.yMax >= _placement.UnlockedArea.yMax - _database.placement.shoreDistance;
        }

        [Test]
        public void NewShoreStand_GoesStraightToTheWater()
        {
            SetMain(LotState.Built, 2);
            int surf = AddStand(Surf);
            EventBus.Publish(new StandCreated(surf, 0));
            Assert.IsTrue(_stands[surf].placed, "moved out of the default row");
            Assert.IsTrue(AtShore(surf), $"at the shore ({_placement.GetStandPosition(surf)})");
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Stand(surf), _placement.GetStandPosition(surf)));
        }

        [Test]
        public void ShoreStand_CannotMoveInland()
        {
            SetMain(LotState.Built, 2);
            int donuts = AddStand("donuts");
            int surf = AddStand(Surf);
            Assert.IsTrue(_placement.CanPlace(PlaceableId.Stand(donuts), Inland), "open ground for other stands");
            Assert.IsFalse(_placement.CanPlace(PlaceableId.Stand(surf), Inland), "too far from the water");
            Assert.IsTrue(_placement.IsShoreOnly(Surf) && !_placement.IsShoreOnly("donuts"));
        }

        [Test]
        public void InlandShoreStand_IsMovedToTheWaterOnLoad()
        {
            SetMain(LotState.Built, 2);
            int surf = AddStand(Surf);
            _stands[surf].placed = true;
            _stands[surf].x = Inland.x;
            _stands[surf].z = Inland.y;
            _placement.EnsureShoreStands();
            Assert.IsTrue(AtShore(surf), $"moved to {_placement.GetStandPosition(surf)}");
        }
    }
}
