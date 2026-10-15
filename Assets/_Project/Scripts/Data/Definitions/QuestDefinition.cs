using UnityEngine;

namespace BeachTycoon.Data
{
    public enum QuestGoal
    {
        SellProduct = 0,
        EarnCoins = 1,
        ServeVisitors = 2,
        PlaceDecorations = 3,
    }

    /// <summary>A simple neighbour request, e.g. "Sell 5 donuts". Progress is tracked from game events.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Quest", fileName = "Quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        public string id;

        [Tooltip("{0} = target amount, {1} = product name.")]
        public string description = "Sell {0} {1}";

        public QuestGoal goal;
        [Tooltip("Only for SellProduct.")]
        public ProductDefinition product;
        [Min(1)] public long target = 5;
        [Min(0)] public long rewardCoins = 100;
        [Tooltip("Only offered once the main building reaches this tier.")]
        [Min(1)] public int minTier = 1;
    }
}
