using System;
using System.Collections.Generic;
using System.Globalization;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>Takes a snapshot of an island: its current objects at their actual positions.</summary>
    public static class IslandSnapshotFactory
    {
        /// <summary>The main building is always included (tier 0 before the tent); houses once they are built.</summary>
        public static IslandSnapshot Capture(string ownerId, string ownerName, string villageName, IReadOnlyList<LotModel> lots,
            IReadOnlyList<StandModel> stands, IReadOnlyList<DecorationModel> decorations, PlacementService placement,
            float popularity, DateTime utcNow)
        {
            var snapshot = new IslandSnapshot
            {
                ownerId = ownerId,
                ownerName = ownerName,
                villageName = villageName ?? string.Empty,
                popularity = popularity,
                timestampUtc = utcNow.ToString("o", CultureInfo.InvariantCulture),
            };

            for (int i = 0; i < lots.Count; i++)
            {
                LotModel lot = lots[i];
                bool main = i == BuildingManager.MainLotIndex;
                if (!main && lot.state != LotState.Built) continue;
                var at = placement.GetLotPosition(i);
                snapshot.buildings.Add(new SnapshotBuilding { lotIndex = i, tier = lot.tier, variant = lot.variant, x = at.x, z = at.y, yaw = lot.yaw });
                if (main) snapshot.tier = lot.tier;
            }

            for (int i = 0; i < stands.Count; i++)
            {
                var at = placement.GetStandPosition(i);
                snapshot.stands.Add(new SnapshotStand { productId = stands[i].productId, level = stands[i].level, x = at.x, z = at.y, yaw = stands[i].yaw, manager = stands[i].manager });
            }

            for (int i = 0; i < decorations.Count; i++)
            {
                var at = placement.GetDecorationPosition(i);
                snapshot.decorations.Add(new SnapshotDecoration { decorationId = decorations[i].decorationId, x = at.x, z = at.y, yaw = decorations[i].yaw });
            }

            return snapshot;
        }
    }
}
