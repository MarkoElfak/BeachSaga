using System;
using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeachTycoon.Tests
{
    /// <summary>In-memory GameDatabase for the online tests: three tiers, three products, two decorations.</summary>
    internal sealed class OnlineTestData : IDisposable
    {
        private readonly List<Object> _assets = new List<Object>();

        public OnlineTestData()
        {
            Database = Asset<GameDatabase>();
            Database.world = Asset<WorldLayoutConfig>();
            Database.terrain = Asset<TerrainConfig>();
            Database.placement = Asset<PlacementConfig>();
            Database.economy = Asset<EconomyConfig>();
            Database.visitors = Asset<VisitorConfig>();
            Database.neighbours = Asset<NeighbourConfig>();
            Database.online = Asset<OnlineConfig>();
            Database.tiers = new[]
            {
                Tier(new Rect(-1.6f, -1.55f, 3.2f, 3.1f), Rect.MinMaxRect(-11.8f, -15.6f, 11.8f, 6.5f)),
                Tier(new Rect(-1.9f, -2f, 3.8f, 4f), Rect.MinMaxRect(-15f, -16f, 15f, 6.5f)),
                Tier(new Rect(-2.2f, -2.6f, 4.4f, 5.1f), Rect.MinMaxRect(-19.2f, -16.9f, 19.2f, 6.5f)),
            };
            Database.products = new[] { Product("donuts", 1), Product("towels", 2), Product("smoothies", 3) };
            Database.decorations = new[] { Decoration("umbrella", 1), Decoration("sandcastle", 2) };
        }

        public GameDatabase Database { get; }

        /// <summary>Adds a product after the three above, such as a country's own place.</summary>
        public ProductDefinition AddProduct(string id, int tier, string cultureId)
        {
            ProductDefinition product = Product(id, tier);
            product.cultureId = cultureId;
            var products = new List<ProductDefinition>(Database.products) { product };
            Database.products = products.ToArray();
            return product;
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

        private TierDefinition Tier(Rect footprint, Rect area)
        {
            var tier = Asset<TierDefinition>();
            tier.footprint = footprint;
            tier.buildArea = area;
            tier.maxVisitors = 20;
            return tier;
        }

        private ProductDefinition Product(string id, int tier)
        {
            var product = Asset<ProductDefinition>();
            product.id = id;
            product.requiredTier = tier;
            product.standFootprint = new Rect(-0.95f, -0.55f, 1.9f, 1.25f);
            product.queueStart = new Vector2(0f, -1.7f);
            return product;
        }

        private DecorationDefinition Decoration(string id, int tier)
        {
            var decoration = Asset<DecorationDefinition>();
            decoration.id = id;
            decoration.requiredTier = tier;
            decoration.area = new Rect(-0.7f, -0.7f, 1.4f, 1.4f);
            return decoration;
        }
    }
}
