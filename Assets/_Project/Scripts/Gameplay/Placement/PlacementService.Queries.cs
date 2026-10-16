using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Area lists for the scenery, the terrain and visitor navigation.</summary>
    public sealed partial class PlacementService
    {
        /// <summary>Areas taken by objects and the stand queues (scenery there is hidden).</summary>
        public void GetOccupiedRects(List<Rect> into)
        {
            into.Clear();
            ForEachPresent(id =>
            {
                Vector2 at = GetPosition(id);
                into.Add(BlockingRect(id, GetFootprint(id, at), at));
            });
        }

        /// <summary>Footprints visitors walk around, grown by 'clearance'.</summary>
        public void GetObstacles(List<Rect> into, float clearance)
        {
            into.Clear();
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                if (IsPresent(PlaceableId.Lot(i))) into.Add(Grow(GetFootprint(PlaceableId.Lot(i), GetLotPosition(i)), clearance));
            }

            for (int i = 0; i < _stands.Count; i++) into.Add(Grow(GetFootprint(PlaceableId.Stand(i), GetStandPosition(i)), clearance));
            for (int i = 0; i < _decorations.Count; i++)
            {
                into.Add(Grow(GetFootprint(PlaceableId.Decoration(i), GetDecorationPosition(i)), clearance));
            }
        }

        private void ForEachPresent(Action<PlaceableId> action)
        {
            for (int i = 0; i < _buildings.LotCount; i++)
            {
                if (IsPresent(PlaceableId.Lot(i))) action(PlaceableId.Lot(i));
            }

            for (int i = 0; i < _stands.Count; i++) action(PlaceableId.Stand(i));
            for (int i = 0; i < _decorations.Count; i++) action(PlaceableId.Decoration(i));
        }

        private static Rect Grow(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);
    }
}
