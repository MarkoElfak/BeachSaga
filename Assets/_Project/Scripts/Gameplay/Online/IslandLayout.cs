using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// A read-only island built from models: its own BuildingManager and PlacementService (which ignores the
    /// home island's events), so footprints, flat ground and walk paths work exactly like at home.
    /// </summary>
    public sealed class IslandLayout : IDisposable
    {
        public IslandLayout(GameDatabase database, List<LotModel> lots, List<StandModel> stands, List<DecorationModel> decorations)
        {
            Lots = lots;
            Stands = stands;
            Decorations = decorations;
            // Nothing is ever bought on a visited island, so it gets an empty wallet of its own.
            Buildings = new BuildingManager(database, new EconomyService(new WalletModel()), lots);
            Placement = new PlacementService(database, Buildings, stands, decorations, false);
        }

        public List<LotModel> Lots { get; }
        public List<StandModel> Stands { get; }
        public List<DecorationModel> Decorations { get; }
        public BuildingManager Buildings { get; }
        public PlacementService Placement { get; }

        /// <summary>Every object stands exactly where the snapshot says (placed = true).</summary>
        public static IslandLayout FromSnapshot(GameDatabase database, IslandSnapshot snapshot)
        {
            var lots = new List<LotModel>();
            foreach (SnapshotBuilding building in snapshot.buildings)
            {
                while (lots.Count <= building.lotIndex) lots.Add(new LotModel(lots.Count));
                LotModel lot = lots[building.lotIndex];
                lot.state = building.tier > 0 ? LotState.Built : LotState.Empty;
                lot.tier = building.tier;
                lot.variant = building.variant;
                lot.placed = true;
                lot.x = building.x;
                lot.z = building.z;
                lot.yaw = building.yaw;
            }

            var stands = new List<StandModel>(snapshot.stands.Count);
            for (int i = 0; i < snapshot.stands.Count; i++)
            {
                SnapshotStand stand = snapshot.stands[i];
                stands.Add(new StandModel
                {
                    productId = stand.productId, lotIndex = BuildingManager.MainLotIndex, slotIndex = i, level = stand.level,
                    placed = true, x = stand.x, z = stand.z, yaw = stand.yaw,
                });
            }

            var decorations = new List<DecorationModel>(snapshot.decorations.Count);
            foreach (SnapshotDecoration decoration in snapshot.decorations)
            {
                decorations.Add(new DecorationModel { decorationId = decoration.decorationId, placed = true, x = decoration.x, z = decoration.z, yaw = decoration.yaw });
            }

            return new IslandLayout(database, lots, stands, decorations);
        }

        public void Dispose() => Placement.Dispose();
    }
}
