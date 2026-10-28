using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Village buildings (M20): sorts every product into its build-bar tab, creates the beach café, the restaurant,
    /// the first-aid station and the chapel the first time (numbers kept after that, tuned in M21), gives the two food
    /// places a mini-game each and each country its own chapel.
    /// </summary>
    internal static class VillageAssetBuilder
    {
        public static void Fill(GameDatabase database)
        {
            var products = new List<ProductDefinition>(database.products ?? new ProductDefinition[0]);
            foreach (ProductDefinition product in products)
            {
                if (product == null || string.IsNullOrEmpty(product.cultureId) || product.category == BuildCategory.Food) continue;
                product.category = BuildCategory.Food; // the countries' own places
                EditorUtility.SetDirty(product);
            }

            ProductDefinition cafe = Building("BeachCafe", "cafe", "Beach café", "coffee and pastries", BuildCategory.Food, StaffSkill.Service,
                tier: 2, unlock: 600, price: 14, serve: 2.5f, upgrade: 60, PlaceholderPrefabBuilder.BeachCafePath);
            ProductDefinition restaurant = Building("Restaurant", "restaurant", "Restaurant", "seafood platters", BuildCategory.Food, StaffSkill.Cooking,
                tier: 3, unlock: 2500, price: 40, serve: 5f, upgrade: 120, PlaceholderPrefabBuilder.RestaurantPath, workers: 2);
            ProductDefinition firstAid = Building("FirstAid", "first_aid", "First aid", "shade, water and plasters", BuildCategory.Services, StaffSkill.Care,
                tier: 2, unlock: 900, price: 0, serve: 1f, upgrade: 80, PlaceholderPrefabBuilder.FirstAidPath,
                service: new ServiceEffect { kind = ServiceKind.FirstAid, reach = 9f, amount = 0.5f, grownTier = 5, grownName = "Hospital", grownScale = 1.5f });
            ProductDefinition chapel = Building("Chapel", "chapel", "Chapel", "weddings by the sea", BuildCategory.Services, StaffSkill.Care,
                tier: 3, unlock: 3000, price: 400, serve: 1f, upgrade: 150, PlaceholderPrefabBuilder.ChapelSpanishPath,
                service: new ServiceEffect { kind = ServiceKind.Weddings, reach = 9f, amount = 0.15f, eventSeconds = 180f, popularity = 0.15f });
            AddOnce(products, cafe);
            AddOnce(products, restaurant);
            AddOnce(products, firstAid);
            AddOnce(products, chapel);
            database.products = products.ToArray();

            Swap(database, "greece", chapel, PlaceholderPrefabBuilder.ChapelGreekPath);
            Swap(database, "italy", chapel, PlaceholderPrefabBuilder.ChapelItalianPath);
            AddMiniGames(database);
        }

        private static void AddMiniGames(GameDatabase database)
        {
            var games = new List<MiniGameDefinition>(database.miniGames ?? new MiniGameDefinition[0]);
            AddOnce(games, MiniGameAssetBuilder.Make(database, "LatteArt", MiniGameKind.PerfectPour, "cafe", "Latte art",
                "Hold to pour the milk and let go while the cup is inside the green band. Don't let it spill!",
                L(4, 3, speed: 0.4f, tolerance: 0.3f), L(5, 3, speed: 0.5f, tolerance: 0.25f), L(6, 3, speed: 0.6f, tolerance: 0.2f),
                L(7, 2, speed: 0.7f, tolerance: 0.16f), L(8, 2, speed: 0.8f, tolerance: 0.13f)));
            AddOnce(games, MiniGameAssetBuilder.Make(database, "TableOrders", MiniGameKind.DonutRush, "restaurant", "Table orders",
                "A table shows the dish they ordered. Tap that dish before they get hungry and leave.",
                L(6, 3, patience: 5f, choices: 3), L(8, 3, patience: 4.5f, choices: 4), L(10, 3, patience: 4f, choices: 4),
                L(12, 2, patience: 3.4f, choices: 5), L(14, 2, patience: 3f, choices: 6)));
            database.miniGames = games.ToArray();
        }

        private static MiniGameLevel L(int target, int lives, float speed = 0f, float patience = 3f, float tolerance = 0.2f, int choices = 3) =>
            MiniGameAssetBuilder.L(target, lives, speed, 1f, patience, tolerance, choices);

        // Village buildings stand apart from the row of stands; their numbers are set once and kept after that.
        private static ProductDefinition Building(string file, string id, string displayName, string description, BuildCategory category,
            StaffSkill skill, int tier, long unlock, long price, float serve, long upgrade, string prefabPath, int workers = 1, ServiceEffect service = null)
        {
            var product = DataAssetBuilder.LoadOrCreate<ProductDefinition>($"{ProjectPaths.Products}/{file}.asset", out bool created);
            if (created)
            {
                product.id = id;
                product.displayName = displayName;
                product.description = description;
                product.category = category;
                product.skill = skill;
                product.requiredTier = tier;
                product.unlockCost = unlock;
                product.price = price;
                product.serveSeconds = serve;
                product.upgradeBaseCost = upgrade;
                product.workerSlots = workers;
                product.freeStanding = true;
                product.service = service ?? new ServiceEffect();
            }

            if (product.standPrefab == null) product.standPrefab = PlaceholderPrefabBuilder.Load<GameObject>(prefabPath);
            EditorUtility.SetDirty(product);
            return product;
        }

        // The country's own look for a shared prefab (kept when one was set in the Inspector).
        private static void Swap(GameDatabase database, string cultureId, ProductDefinition product, string replacementPath)
        {
            CultureDefinition culture = database.FindCulture(cultureId);
            var replacement = PlaceholderPrefabBuilder.Load<GameObject>(replacementPath);
            if (culture == null || product.standPrefab == null || replacement == null) return;
            var swaps = new List<PrefabSwap>(culture.prefabSwaps ?? new PrefabSwap[0]);
            if (swaps.Exists(s => s.original == product.standPrefab)) return;
            swaps.Add(new PrefabSwap { original = product.standPrefab, replacement = replacement });
            culture.prefabSwaps = swaps.ToArray();
            EditorUtility.SetDirty(culture);
        }

        private static void AddOnce<T>(List<T> list, T item)
        {
            if (!list.Contains(item)) list.Add(item);
        }
    }
}
