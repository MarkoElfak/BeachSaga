using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Products, tiers and visitor types with the design doc's starting numbers. Numbers are written only
    /// when an asset is created; missing art references are filled in on every run.
    /// </summary>
    internal static class ContentAssetBuilder
    {
        private const string VisitorTypes = ProjectPaths.Data + "/VisitorTypes";

        public static void Fill(GameDatabase database)
        {
            ProjectPaths.EnsureFolder(ProjectPaths.Products);
            ProjectPaths.EnsureFolder(ProjectPaths.Tiers);
            ProjectPaths.EnsureFolder(VisitorTypes);

            ProductDefinition donuts = Product("Donuts", "donuts", "Donuts", 0, 5, 3f, 1, 25, PlaceholderPrefabBuilder.DonutStandPath);
            ProductDefinition towels = Product("Towels", "towels", "Towels", 200, 12, 2f, 2, 60, PlaceholderPrefabBuilder.TowelStandPath);
            ProductDefinition drinks = Product("Drinks", "drinks", "Drinks", 800, 20, 3f, 3, 100, PlaceholderPrefabBuilder.DrinksBarPath);
            ProductDefinition surf = Product("SurfLessons", "surf_lessons", "Surf lessons", 3000, 60, 8f, 4, 300, PlaceholderPrefabBuilder.SurfSchoolPath);
            ProductDefinition houses = Product("Houses", "houses", "Houses", 25000, 5000, 20f, 5, 25000, PlaceholderPrefabBuilder.RealEstatePath);

            ManagerCost(donuts, 500);
            ManagerCost(towels, 1500);
            ManagerCost(drinks, 5000);
            ManagerCost(surf, 15000);
            ManagerCost(houses, 100000);
            database.products = new[] { donuts, towels, drinks, surf, houses };
            database.tiers = new[]
            {
                Tier("1_Tent", "tent", "Tent", PlaceholderPrefabBuilder.TentPath, 0, 2f, 2, donuts, 0, 0f),
                Tier("2_Cabin", "cabin", "Cabin", PlaceholderPrefabBuilder.CabinPath, 150, 4f, 6, towels, 1, 0.25f),
                Tier("3_Bungalow", "bungalow", "Bungalow", PlaceholderPrefabBuilder.BungalowPath, 1200, 6f, 15, drinks, 3, 0.5f),
                Tier("4_Hotel", "hotel", "Hotel", PlaceholderPrefabBuilder.HotelPath, 9000, 8f, 40, surf, 8, 1f),
                Tier("5_Resort", "resort", "Resort", PlaceholderPrefabBuilder.ResortPath, 60000, 10f, 100, houses, 20, 2f),
            };
            for (int i = 0; i < database.tiers.Length; i++) SetLand(database.tiers[i], i);

            var types = new List<VisitorTypeDefinition>
            {
                Visitor("Tourist", 6f, 1f, 20f, 1, donuts),
                Visitor("Foodie", 2f, 1.5f, 14f, 1, donuts),
                Visitor("Sunbather", 4f, 1.2f, 25f, 2, towels),
                Visitor("Partygoer", 4f, 1.3f, 18f, 3, drinks),
                Visitor("Surfer", 3f, 1.4f, 30f, 4, surf),
                Visitor("Investor", 1f, 1f, 45f, 5, houses),
            };
            database.visitorTypes = types.ToArray();
        }

        // Land per tier: (half width, bottom z, zoom-out limit). The top edge stays at the wet sand.
        private const float LandTopZ = 6.5f;
        private static readonly Vector3[] Land =
        {
            new Vector3(11.8f, -15.6f, 44f), new Vector3(13.8f, -16f, 50f), new Vector3(15.8f, -16.3f, 56f),
            new Vector3(17.6f, -16.6f, 62f), new Vector3(19.2f, -16.9f, 70f),
        };

        // Only fills land that was never set, so tuned areas survive re-running Setup.
        private static void SetLand(TierDefinition tier, int index)
        {
            if (tier.buildArea.width > 0f || index >= Land.Length) return;
            Vector3 land = Land[index];
            tier.buildArea = Rect.MinMaxRect(-land.x, land.y, land.x, LandTopZ);
            tier.maxZoomDistance = land.z;
            EditorUtility.SetDirty(tier);
        }

        // Managers came later (M8): products without a manager cost get the default one.
        private static void ManagerCost(ProductDefinition product, long cost)
        {
            if (product.managerCost > 0) return;
            product.managerCost = cost;
            EditorUtility.SetDirty(product);
        }

        private static ProductDefinition Product(string file, string id, string displayName, long unlockCost,
            long price, float serveSeconds, int requiredTier, long upgradeBaseCost, string standPrefabPath)
        {
            var product = DataAssetBuilder.LoadOrCreate<ProductDefinition>($"{ProjectPaths.Products}/{file}.asset", out bool created);
            if (created)
            {
                product.id = id;
                product.displayName = displayName;
                product.unlockCost = unlockCost;
                product.price = price;
                product.serveSeconds = serveSeconds;
                product.requiredTier = requiredTier;
                product.upgradeBaseCost = upgradeBaseCost;
            }

            if (product.standPrefab == null && standPrefabPath != null)
            {
                product.standPrefab = PlaceholderPrefabBuilder.Load<GameObject>(standPrefabPath);
            }

            EditorUtility.SetDirty(product);
            return product;
        }

        private static TierDefinition Tier(string file, string id, string displayName, string prefabPath, long cost,
            float buildSeconds, int maxVisitors, ProductDefinition product, int neighbourSlots, float popularity)
        {
            var tier = DataAssetBuilder.LoadOrCreate<TierDefinition>($"{ProjectPaths.Tiers}/{file}.asset", out bool created);
            if (created)
            {
                tier.id = id;
                tier.displayName = displayName;
                tier.upgradeCost = cost;
                tier.buildSeconds = buildSeconds;
                tier.maxVisitors = maxVisitors;
                tier.unlockedProducts = new[] { product };
                tier.neighbourSlots = neighbourSlots;
                tier.popularityBonus = popularity;
            }

            if (tier.buildingPrefab == null && prefabPath != null)
            {
                tier.buildingPrefab = PlaceholderPrefabBuilder.Load<GameObject>(prefabPath);
            }

            EditorUtility.SetDirty(tier);
            return tier;
        }

        private static VisitorTypeDefinition Visitor(string name, float weight, float spend, float patience,
            int minTier, ProductDefinition preferred)
        {
            var type = DataAssetBuilder.LoadOrCreate<VisitorTypeDefinition>($"{VisitorTypes}/{name}.asset", out bool created);
            if (created)
            {
                type.id = name.ToLowerInvariant();
                type.spawnWeight = weight;
                type.spendMultiplier = spend;
                type.patienceSeconds = patience;
                type.minTier = minTier;
                type.preferredProducts = new[] { preferred };
            }

            if (type.prefab == null)
            {
                type.prefab = PlaceholderPrefabBuilder.Load<GameObject>(PlaceholderPrefabBuilder.VisitorPath(name));
            }

            EditorUtility.SetDirty(type);
            return type;
        }
    }
}
