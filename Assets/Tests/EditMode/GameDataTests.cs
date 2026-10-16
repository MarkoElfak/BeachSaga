using System.Collections.Generic;
using System.Reflection;
using BeachTycoon.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.Tests
{
    /// <summary>Sanity checks on the real data assets (run Beach Tycoon > Setup Project first).</summary>
    public sealed class GameDataTests
    {
        private GameDatabase _database;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(GameDatabase));
            if (guids.Length == 0) Assert.Ignore("No GameDatabase asset yet; run Beach Tycoon > Setup Project.");
            _database = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        [Test]
        public void EveryConfigIsAssigned()
        {
            foreach (FieldInfo field in typeof(GameDatabase).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (typeof(ScriptableObject).IsAssignableFrom(field.FieldType))
                {
                    Assert.IsNotNull(field.GetValue(_database) as Object, field.Name);
                }
            }
        }

        [Test]
        public void TiersAreOrderedAndPriced()
        {
            Assert.Greater(_database.MaxTier, 0);
            for (int tier = 1; tier <= _database.MaxTier; tier++)
            {
                TierDefinition definition = _database.GetTier(tier);
                Assert.IsNotNull(definition, $"tier {tier}");
                Assert.IsNotNull(definition.buildingPrefab, $"tier {tier} prefab");
                Assert.Greater(definition.maxVisitors, 0, $"tier {tier} visitors");
                if (tier > 1) Assert.Greater(definition.upgradeCost, _database.GetTier(tier - 1).upgradeCost, $"tier {tier} cost");
            }
        }

        [Test]
        public void ProductsHaveUniqueIdsAndPositivePrices()
        {
            var ids = new HashSet<string>();
            foreach (ProductDefinition product in _database.products)
            {
                Assert.IsNotNull(product);
                Assert.IsTrue(ids.Add(product.id), $"duplicate product id {product.id}");
                Assert.Greater(product.price, 0, product.id);
                Assert.Greater(product.serveSeconds, 0f, product.id);
                Assert.IsNotNull(product.standPrefab, product.id);
            }
        }

        [Test]
        public void DecorationsHaveUniqueIdsPrefabsAndFootprints()
        {
            var ids = new HashSet<string>();
            foreach (DecorationDefinition decoration in _database.decorations)
            {
                Assert.IsNotNull(decoration);
                Assert.IsTrue(ids.Add(decoration.id), $"duplicate decoration id {decoration.id}");
                Assert.IsNotNull(decoration.prefab, decoration.id);
                Assert.Greater(decoration.footprint.x, 0f, decoration.id);
                Assert.Greater(decoration.footprint.y, 0f, decoration.id);
                Assert.AreSame(decoration, _database.FindDecoration(decoration.id));
            }
        }

        [Test]
        public void OfflineCapIsPositive()
        {
            Assert.Greater(_database.economy.offlineCapHours, 0f);
            Assert.That(_database.economy.offlineEfficiency, Is.InRange(0f, 1f));
        }
    }
}
