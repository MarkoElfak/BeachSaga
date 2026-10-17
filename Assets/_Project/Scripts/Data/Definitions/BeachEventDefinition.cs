using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>
    /// A timed beach event such as a surf contest or a tourist bus. Every effect is optional: a price
    /// multiplier and extra demand for one product, a faster visitor spawn rate and a burst of visitors.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Beach Event", fileName = "BeachEvent")]
    public sealed class BeachEventDefinition : ScriptableObject
    {
        public string id;
        public string title = "Beach event";
        [Tooltip("Shown on the banner under the title.")]
        public string description = string.Empty;
        public Color bannerColor = new Color(0.98f, 0.62f, 0.3f);

        [Min(1f)] public float durationSeconds = 90f;
        [Tooltip("Relative chance of being picked among the events that can run.")]
        [Min(0f)] public float weight = 1f;
        [Min(1)] public int minTier = 1;

        [Header("Product effects (needs a stand for this product to run)")]
        public ProductDefinition product;
        [Tooltip("The product's sale value is multiplied by this during the event.")]
        [Min(0f)] public float priceMultiplier = 1f;
        [Tooltip("Chance that a visitor picking a stand goes for this product first (0 = no change).")]
        [Range(0f, 1f)] public float demandChance;

        [Header("Visitor effects")]
        [Tooltip("Visitors arrive this many times as often during the event.")]
        [Min(0.1f)] public float spawnRateMultiplier = 1f;
        [Tooltip("Visitors who arrive at once when the event starts (may go past the tier's cap; the pool still caps them).")]
        [Min(0)] public int burstVisitors;
    }
}
