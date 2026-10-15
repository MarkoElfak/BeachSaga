using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Milestone 2 content: neighbour settings and house prefabs, quests and decorations.</summary>
    internal static class CommunityAssetBuilder
    {
        private const string Quests = ProjectPaths.Data + "/Quests";
        private const string Decorations = ProjectPaths.Data + "/Decorations";

        public static void Fill(GameDatabase database)
        {
            ProjectPaths.EnsureFolder(Quests);
            ProjectPaths.EnsureFolder(Decorations);

            var neighbours = DataAssetBuilder.LoadOrCreate<NeighbourConfig>(ProjectPaths.Config + "/NeighbourConfig.asset", out _);
            if (neighbours.housePrefabs == null || neighbours.housePrefabs.Length == 0)
            {
                neighbours.housePrefabs = new GameObject[HousePrefabs.Variants.Length];
                for (int i = 0; i < neighbours.housePrefabs.Length; i++)
                {
                    neighbours.housePrefabs[i] = PlaceholderPrefabBuilder.Load<GameObject>(PlaceholderPrefabBuilder.HousePath(i));
                }
            }

            EditorUtility.SetDirty(neighbours);
            database.neighbours = neighbours;

            ProductDefinition donuts = database.FindProduct("donuts");
            ProductDefinition towels = database.FindProduct("towels");
            ProductDefinition drinks = database.FindProduct("drinks");
            ProductDefinition surf = database.FindProduct("surf_lessons");
            ProductDefinition houses = database.FindProduct("houses");
            const string sell = "Sell {0} {1}";
            database.quests = new[]
            {
                Quest("SellDonuts", sell, QuestGoal.SellProduct, donuts, 8, 80, 2),
                Quest("HappyVisitors", "Make {0} visitors happy", QuestGoal.ServeVisitors, null, 15, 150, 2),
                Quest("SellTowels", sell, QuestGoal.SellProduct, towels, 6, 120, 2),
                Quest("EarnCoins", "Earn {0} coins", QuestGoal.EarnCoins, null, 600, 200, 2),
                Quest("Decorate", "Place {0} decorations", QuestGoal.PlaceDecorations, null, 2, 300, 2),
                Quest("SellDrinks", sell, QuestGoal.SellProduct, drinks, 10, 450, 3),
                Quest("SurfLessons", sell, QuestGoal.SellProduct, surf, 5, 1500, 4),
                Quest("SellHouse", sell, QuestGoal.SellProduct, houses, 1, 9000, 5),
            };

            database.decorations = new[]
            {
                Decoration("BeachUmbrella", "Beach umbrella", 120, 0.05f, 1, 3),
                Decoration("DeckChairs", "Deck chairs", 250, 0.06f, 2, 2),
                Decoration("Sandcastle", "Sandcastle", 400, 0.08f, 2, 2),
                Decoration("VolleyballNet", "Volleyball net", 1500, 0.15f, 3, 1),
                Decoration("LifeguardTower", "Lifeguard tower", 4000, 0.25f, 4, 2),
            };
        }

        private static QuestDefinition Quest(string id, string description, QuestGoal goal, ProductDefinition product,
            long target, long reward, int minTier)
        {
            var quest = DataAssetBuilder.LoadOrCreate<QuestDefinition>($"{Quests}/{id}.asset", out bool created);
            if (created)
            {
                quest.id = id;
                quest.description = description;
                quest.goal = goal;
                quest.product = product;
                quest.target = target;
                quest.rewardCoins = reward;
                quest.minTier = minTier;
            }

            EditorUtility.SetDirty(quest);
            return quest;
        }

        private static DecorationDefinition Decoration(string id, string displayName, long cost, float popularity,
            int requiredTier, int maxCount)
        {
            var decoration = DataAssetBuilder.LoadOrCreate<DecorationDefinition>($"{Decorations}/{id}.asset", out bool created);
            if (created)
            {
                decoration.id = id;
                decoration.displayName = displayName;
                decoration.cost = cost;
                decoration.popularity = popularity;
                decoration.requiredTier = requiredTier;
                decoration.maxCount = maxCount;
            }

            if (decoration.prefab == null)
            {
                decoration.prefab = PlaceholderPrefabBuilder.Load<GameObject>(PlaceholderPrefabBuilder.DecorationPath(id));
            }

            EditorUtility.SetDirty(decoration);
            return decoration;
        }
    }
}
