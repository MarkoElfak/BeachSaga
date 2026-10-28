using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Country abilities: gives Greece, Spain and Italy their bonus (unless one was set in the Inspector) and creates
    /// each country's own place the first time (numbers kept after that), listed after the shared products, with a
    /// mini-game each that reuses a shared game's rules.
    /// </summary>
    internal static class AbilityAssetBuilder
    {
        public static void Fill(GameDatabase database)
        {
            Ability(database, "greece", "Filoxenia", CultureAbilityKind.LongerStays, 0.2f);
            Ability(database, "spain", "Fiesta", CultureAbilityKind.NightAndEvents, 0.25f);
            Ability(database, "italy", "Fatto a mano", CultureAbilityKind.HigherPrices, 0.1f);

            var products = new List<ProductDefinition>(database.products ?? new ProductDefinition[0]);
            AddOnce(products, Place("Taverna", "taverna", "Taverna", "greece", "souvlaki and moussaka", PlaceholderPrefabBuilder.TavernaPath));
            AddOnce(products, Place("TapasBar", "tapas_bar", "Tapas bar", "spain", "tapas and paella", PlaceholderPrefabBuilder.TapasBarPath));
            AddOnce(products, Place("Gelateria", "gelateria", "Gelateria", "italy", "gelato and pizza slices", PlaceholderPrefabBuilder.GelateriaPath));
            database.products = products.ToArray();
            AddMiniGames(database);
        }

        private static void AddMiniGames(GameDatabase database)
        {
            var games = new List<MiniGameDefinition>(database.miniGames ?? new MiniGameDefinition[0]);
            AddOnce(games, MiniGameAssetBuilder.Make(database, "MoussakaLayers", MiniGameKind.SkyBuilder, "taverna", "Moussaka layers",
                "A new layer slides from side to side. Tap to drop it right on top of the moussaka.",
                L(5, 3, speed: 0.5f, tolerance: 0.3f), L(6, 3, speed: 0.65f, tolerance: 0.25f), L(8, 3, speed: 0.8f, tolerance: 0.2f),
                L(10, 2, speed: 1f, tolerance: 0.16f), L(12, 2, speed: 1.2f, tolerance: 0.12f)));
            AddOnce(games, MiniGameAssetBuilder.Make(database, "SangriaPour", MiniGameKind.PerfectPour, "tapas_bar", "Sangria pour",
                "Hold to fill the jug and let go while the sangria is inside the green band. Don't let it overflow!",
                L(4, 3, speed: 0.35f, tolerance: 0.3f), L(5, 3, speed: 0.45f, tolerance: 0.25f), L(6, 3, speed: 0.55f, tolerance: 0.2f),
                L(7, 2, speed: 0.65f, tolerance: 0.16f), L(8, 2, speed: 0.75f, tolerance: 0.13f)));
            AddOnce(games, MiniGameAssetBuilder.Make(database, "GelatoRush", MiniGameKind.DonutRush, "gelateria", "Gelato rush",
                "A customer shows the flavour they want. Tap that flavour before their patience runs out.",
                L(6, 3, patience: 5f, choices: 3), L(8, 3, patience: 4f, choices: 3), L(10, 3, patience: 3.5f, choices: 4),
                L(12, 2, patience: 3f, choices: 5), L(14, 2, patience: 2.6f, choices: 6)));
            database.miniGames = games.ToArray();
        }

        private static MiniGameLevel L(int target, int lives, float speed = 0f, float pace = 1f, float patience = 3f, float tolerance = 0.2f,
            int choices = 3) => MiniGameAssetBuilder.L(target, lives, speed, pace, patience, tolerance, choices);

        private static void Ability(GameDatabase database, string cultureId, string name, CultureAbilityKind kind, float amount)
        {
            CultureDefinition culture = database.FindCulture(cultureId);
            if (culture == null || (culture.ability != null && culture.ability.kind != CultureAbilityKind.None)) return;
            culture.ability = new CultureAbility { name = name, kind = kind, amount = amount };
            EditorUtility.SetDirty(culture);
        }

        // Opens at the Cabin like the towels; every place has the same numbers so no country starts ahead.
        private static ProductDefinition Place(string file, string id, string displayName, string cultureId, string description, string prefabPath)
        {
            var product = DataAssetBuilder.LoadOrCreate<ProductDefinition>($"{ProjectPaths.Products}/{file}.asset", out bool created);
            if (created)
            {
                product.id = id;
                product.displayName = displayName;
                product.description = description;
                product.cultureId = cultureId;
                product.unlockCost = 400;
                product.price = 15;
                product.serveSeconds = 3f;
                product.requiredTier = 2;
                product.upgradeBaseCost = 70;
                product.skill = StaffSkill.Cooking;
            }

            if (product.standPrefab == null) product.standPrefab = PlaceholderPrefabBuilder.Load<GameObject>(prefabPath);
            EditorUtility.SetDirty(product);
            return product;
        }

        private static void AddOnce<T>(List<T> list, T item)
        {
            if (!list.Contains(item)) list.Add(item);
        }
    }
}
