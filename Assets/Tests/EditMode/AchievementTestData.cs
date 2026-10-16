using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeachTycoon.Tests
{
    /// <summary>In-memory achievement definitions for the achievement tests.</summary>
    internal sealed class AchievementTestData : IDisposable
    {
        private readonly List<Object> _assets = new List<Object>();

        public AchievementTestData()
        {
            Donuts = Asset<ProductDefinition>();
            Donuts.id = "donuts";
            Donuts.displayName = "Donuts";
            Towels = Asset<ProductDefinition>();
            Towels.id = "towels";
        }

        public ProductDefinition Donuts { get; }
        public ProductDefinition Towels { get; }

        public AchievementDefinition Make(string id, AchievementGoal goal, long target, long coins = 100,
            float boostMinutes = 0f, ProductDefinition product = null, string description = "Do {0} {1}")
        {
            var achievement = Asset<AchievementDefinition>();
            achievement.id = id;
            achievement.title = id;
            achievement.goal = goal;
            achievement.target = target;
            achievement.rewardCoins = coins;
            achievement.rewardBoostMinutes = boostMinutes;
            achievement.product = product;
            achievement.description = description;
            return achievement;
        }

        public void Dispose()
        {
            foreach (Object asset in _assets) Object.DestroyImmediate(asset);
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }
    }
}
