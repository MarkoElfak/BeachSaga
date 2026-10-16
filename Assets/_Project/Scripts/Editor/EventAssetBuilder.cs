using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Milestone 9 content: the day and night config and the beach events. Values are written only on creation.</summary>
    internal static class EventAssetBuilder
    {
        private const string Folder = ProjectPaths.Data + "/Events";

        public static void Fill(GameDatabase database)
        {
            database.dayNight = DataAssetBuilder.LoadOrCreate<DayNightConfig>(ProjectPaths.Config + "/DayNightConfig.asset", out _);
            BeachEventConfig config = DataAssetBuilder.LoadOrCreate<BeachEventConfig>(ProjectPaths.Config + "/BeachEventConfig.asset", out _);
            database.beachEvents = config;

            ProjectPaths.EnsureFolder(Folder);
            ProductDefinition drinks = database.FindProduct("drinks");
            ProductDefinition surf = database.FindProduct("surf_lessons");
            config.events = new[]
            {
                Make("TouristBus", "Tourist bus", "More visitors, arriving twice as often", new Color(0.98f, 0.85f, 0.35f), 90f, 1,
                    null, 1f, 0f, 2f, 8),
                Make("Heatwave", "Heatwave", "Everyone wants drinks, at 1.5x the price", new Color(1f, 0.6f, 0.4f), 150f, 3,
                    drinks, 1.5f, 0.6f, 1f, 0),
                Make("SurfContest", "Surf contest", "Surf lessons sell for 2x", new Color(0.45f, 0.78f, 0.98f), 120f, 4,
                    surf, 2f, 0f, 1f, 0),
            };
            EditorUtility.SetDirty(config);
        }

        private static BeachEventDefinition Make(string id, string title, string description, Color color, float seconds,
            int minTier, ProductDefinition product, float price, float demand, float spawnRate, int burst)
        {
            var definition = DataAssetBuilder.LoadOrCreate<BeachEventDefinition>($"{Folder}/{id}.asset", out bool created);
            if (created)
            {
                definition.id = id;
                definition.title = title;
                definition.description = description;
                definition.bannerColor = color;
                definition.durationSeconds = seconds;
                definition.minTier = minTier;
                definition.product = product;
                definition.priceMultiplier = price;
                definition.demandChance = demand;
                definition.spawnRateMultiplier = spawnRate;
                definition.burstVisitors = burst;
            }

            EditorUtility.SetDirty(definition);
            return definition;
        }
    }
}
