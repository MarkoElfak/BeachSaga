using BeachTycoon.Data;
using UnityEditor;

namespace BeachTycoon.EditorTools
{
    /// <summary>Milestone 7 content: long-term achievements. Values are written only when an asset is first created.</summary>
    internal static class AchievementAssetBuilder
    {
        private const string Folder = ProjectPaths.Data + "/Achievements";
        private const string ManagersText = "Have {0} stands run by level 3 workers";

        public static void Fill(GameDatabase database)
        {
            ProjectPaths.EnsureFolder(Folder);
            ProductDefinition donuts = database.FindProduct("donuts");
            ProductDefinition surf = database.FindProduct("surf_lessons");
            database.achievements = new[]
            {
                Make("Coins10K", "Beach business", "Earn {0} coins", AchievementGoal.EarnCoins, null, 10000, 1000, 0f),
                Make("Donuts100", "Donut king", "Sell {0} {1}", AchievementGoal.SellProduct, donuts, 100, 800, 0f),
                Make("Visitors250", "Crowd pleaser", "Serve {0} visitors", AchievementGoal.ServeVisitors, null, 250, 2500, 10f),
                Make("StandLevel10", "Top stand", "Upgrade a stand to level {0}", AchievementGoal.StandLevel, null, 10, 2000, 0f),
                Make("Decor10", "Decorator", "Place {0} decorations", AchievementGoal.OwnDecorations, null, 10, 5000, 0f),
                Make("Hotel", "Hotelier", "Open the Hotel", AchievementGoal.ReachTier, null, 4, 20000, 15f),
                Make("Neighbours5", "Good neighbour", "Host {0} neighbours", AchievementGoal.HostNeighbours, null, 5, 8000, 0f),
                Make("Quests10", "Helping hand", "Finish {0} neighbour requests", AchievementGoal.CompleteQuests, null, 10, 6000, 0f),
                Make("Visits3", "Globetrotter", "Visit {0} friends' islands", AchievementGoal.VisitIslands, null, 3, 1500, 5f),
                Make("Streak7", "Loyal local", "Log in {0} days in a row", AchievementGoal.DailyStreak, null, 7, 10000, 20f),
                Make("Managers3", "Delegator", ManagersText, AchievementGoal.HireManagers, null, 3, 4000, 10f),
                Make("Surf50", "Surf school", "Sell {0} {1}", AchievementGoal.SellProduct, surf, 50, 25000, 0f),
                Make("Resort", "Resort mogul", "Open the Resort", AchievementGoal.ReachTier, null, 5, 150000, 30f),
                Make("Coins1M", "Millionaire", "Earn {0} coins", AchievementGoal.EarnCoins, null, 1000000, 100000, 30f),
            };
        }

        private static AchievementDefinition Make(string id, string title, string description, AchievementGoal goal,
            ProductDefinition product, long target, long rewardCoins, float boostMinutes)
        {
            var achievement = DataAssetBuilder.LoadOrCreate<AchievementDefinition>($"{Folder}/{id}.asset", out bool created);
            if (created)
            {
                achievement.id = id;
                achievement.title = title;
                achievement.description = description;
                achievement.goal = goal;
                achievement.product = product;
                achievement.target = target;
                achievement.rewardCoins = rewardCoins;
                achievement.rewardBoostMinutes = boostMinutes;
            }

            // Managers merged into staff (M18), so the old wording is replaced.
            if (achievement.goal == AchievementGoal.HireManagers) achievement.description = description;

            EditorUtility.SetDirty(achievement);
            return achievement;
        }
    }
}
