using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Mock friends until there is a backend: builds each demo friend's island from OnlineConfig (tier, houses,
    /// the stands that tier allows, decorations on the default spots), moves anything that overlaps to a free
    /// spot, and stores it in the repository like a real snapshot.
    /// </summary>
    public static class DemoIslands
    {
        public static List<FriendInfo> Friends(OnlineConfig config)
        {
            var friends = new List<FriendInfo>();
            foreach (DemoIsland demo in config != null && config.demoFriends != null ? config.demoFriends : new DemoIsland[0])
            {
                if (!string.IsNullOrEmpty(demo.ownerId)) friends.Add(new FriendInfo(demo.ownerId, demo.displayName, demo.tier));
            }

            return friends;
        }

        public static void Seed(GameDatabase database, IIslandRepository repository, DateTime utcNow)
        {
            if (database.online == null || database.online.demoFriends == null) return;
            foreach (DemoIsland demo in database.online.demoFriends)
            {
                if (!string.IsNullOrEmpty(demo.ownerId)) repository.SaveSnapshot(Create(database, demo, utcNow), null);
            }
        }

        public static IslandSnapshot Create(GameDatabase database, DemoIsland demo, DateTime utcNow)
        {
            int tier = Mathf.Clamp(demo.tier, 1, Mathf.Max(1, database.MaxTier));
            var layout = new IslandLayout(database, new List<LotModel>(), new List<StandModel>(), new List<DecorationModel>());
            LotModel main = layout.Buildings.GetLot(BuildingManager.MainLotIndex);
            main.state = LotState.Built;
            main.tier = tier;
            Settle(layout.Placement, PlaceableId.Lot(BuildingManager.MainLotIndex), (x, z) => Place(main, x, z));

            for (int lot = 1; lot <= demo.houses && lot < layout.Buildings.LotCount; lot++)
            {
                LotModel house = layout.Buildings.GetLot(lot);
                house.state = LotState.Built;
                house.tier = 1;
                house.variant = lot - 1;
                if (!Settle(layout.Placement, PlaceableId.Lot(lot), (x, z) => Place(house, x, z))) house.state = LotState.Empty;
            }

            AddStands(database, layout, tier);
            AddDecorations(database, layout, tier, demo.decorations);
            IslandSnapshot snapshot = IslandSnapshotFactory.Capture(demo.ownerId, demo.displayName, demo.villageName, layout.Lots,
                layout.Stands, layout.Decorations, layout.Placement, demo.popularity, utcNow);
            layout.Dispose();
            return snapshot;
        }

        private static void AddStands(GameDatabase database, IslandLayout layout, int tier)
        {
            foreach (ProductDefinition product in database.products ?? new ProductDefinition[0])
            {
                if (product == null || product.requiredTier > tier) continue;
                var stand = new StandModel { productId = product.id, slotIndex = layout.Stands.Count, level = Mathf.Min(tier - 1, 3) };
                layout.Stands.Add(stand);
                if (!Settle(layout.Placement, PlaceableId.Stand(layout.Stands.Count - 1), (x, z) => Place(stand, x, z)))
                {
                    layout.Stands.RemoveAt(layout.Stands.Count - 1);
                }
            }
        }

        private static void AddDecorations(GameDatabase database, IslandLayout layout, int tier, int count)
        {
            Vector2[] spots = database.world.decorationSpots ?? new Vector2[0];
            DecorationDefinition[] all = database.decorations ?? new DecorationDefinition[0];
            for (int i = 0, spot = 0; i < all.Length * spots.Length && layout.Decorations.Count < count && spot < spots.Length; i++)
            {
                DecorationDefinition definition = all[i % all.Length];
                if (definition == null || definition.requiredTier > tier) continue;
                var decoration = new DecorationModel { decorationId = definition.id, spotIndex = spot++ };
                layout.Decorations.Add(decoration);
                if (!Settle(layout.Placement, PlaceableId.Decoration(layout.Decorations.Count - 1), (x, z) => Place(decoration, x, z)))
                {
                    layout.Decorations.RemoveAt(layout.Decorations.Count - 1);
                }
            }
        }

        // Keeps the default position when it is free, else the nearest free spot; false when there is none.
        private static bool Settle(PlacementService placement, PlaceableId id, Action<float, float> place)
        {
            Vector2 at = placement.GetPosition(id);
            if (!placement.CanPlace(id, at) && !placement.TryFindFreeSpot(id, at, out at)) return false;
            place(at.x, at.y);
            return true;
        }

        private static void Place(LotModel lot, float x, float z) { lot.placed = true; lot.x = x; lot.z = z; }
        private static void Place(StandModel stand, float x, float z) { stand.placed = true; stand.x = x; stand.z = z; }
        private static void Place(DecorationModel decoration, float x, float z) { decoration.placed = true; decoration.x = x; decoration.z = z; }
    }
}
