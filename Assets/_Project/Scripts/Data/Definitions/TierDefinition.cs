using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>One step of the main building (Tent, Cabin, Bungalow, Hotel, Resort).</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Tier", fileName = "Tier")]
    public sealed class TierDefinition : ScriptableObject
    {
        [Tooltip("Stable id used in save files.")]
        public string id;
        public string displayName;

        [Tooltip("Prefab with a BuildingView component.")]
        public GameObject buildingPrefab;

        [Tooltip("Coins to reach this tier from the previous one. For tier 1 this is the build cost.")]
        [Min(0)] public long upgradeCost;

        [Tooltip("Construction time in seconds.")]
        [Min(0f)] public float buildSeconds = 2f;

        [Min(0)] public int maxVisitors;
        public ProductDefinition[] unlockedProducts;
        [Min(0)] public int neighbourSlots;
        [Min(0f)] public float popularityBonus;

        [Header("Placement")]
        [Tooltip("Ground area the building covers (x, z relative to the lot). Measured from the prefab by Setup " +
                 "while Auto Footprint is on.")]
        public Rect footprint = new Rect(-1.5f, -1.5f, 3f, 3f);
        public bool autoFootprint = true;

        [Header("Land")]
        [Tooltip("Buildable area (x, z, width, depth) once this tier is reached. Each upgrade opens more land.")]
        public Rect buildArea;
    }
}
