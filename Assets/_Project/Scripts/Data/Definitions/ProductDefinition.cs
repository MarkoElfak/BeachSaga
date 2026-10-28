using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Something a stand sells. Stand art is swapped through standPrefab.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Product", fileName = "Product")]
    public sealed class ProductDefinition : ScriptableObject
    {
        [Tooltip("Stable id used in save files. Never change after release.")]
        public string id;
        public string displayName;
        [Tooltip("What it sells, in a few words (shown on cards), such as \"souvlaki and moussaka\".")]
        public string description;
        public Sprite icon;

        [Tooltip("A country's own place: only villages of this culture can open it. Empty = every village.")]
        public string cultureId;

        [Tooltip("Coins needed to unlock this product once its tier is reached. 0 = free.")]
        [Min(0)] public long unlockCost;

        [Tooltip("Base coins earned per sale, before visitor and stand-level multipliers.")]
        [Min(0)] public long price;

        [Min(0.1f)] public float serveSeconds = 1f;

        [Tooltip("Stand upgrade cost at level 0. Each level multiplies it by EconomyConfig.upgradeCostGrowth.")]
        [Min(0)] public long upgradeBaseCost = 25;

        [Tooltip("What a worker here needs to be good at; their stars in it make the stand serve faster.")]
        public StaffSkill skill;

        [Tooltip("Building tier (1 = Tent) required before the product can be sold.")]
        [Min(1)] public int requiredTier = 1;

        [Tooltip("Prefab with a StandView component, spawned next to the building.")]
        public GameObject standPrefab;

        [Header("Placement")]
        [Tooltip("Ground area of the stand (x, z relative to its pivot). Measured from the prefab by Setup " +
                 "while Auto Footprint is on.")]
        public Rect standFootprint = new Rect(-1f, -0.75f, 2f, 1.5f);
        [Tooltip("Where the queue starts (x, z relative to the pivot); it grows toward -z. Measured by Setup.")]
        public Vector2 queueStart = new Vector2(0f, -1.7f);
        public bool autoFootprint = true;

        /// <summary>A country's place is open only to that country; everything else to every village.</summary>
        public bool IsOpenTo(string villageCultureId) => string.IsNullOrEmpty(cultureId) || cultureId == villageCultureId;
    }
}
