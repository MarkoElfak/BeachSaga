using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>A purchasable beach decoration. Each one placed adds popularity.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Decoration", fileName = "Decoration")]
    public sealed class DecorationDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [Min(0)] public long cost = 100;
        [Tooltip("Popularity added per placed copy (on top of VisitorConfig.popularityPerDecoration).")]
        [Min(0f)] public float popularity = 0.05f;
        [Min(1)] public int requiredTier = 1;
        [Min(1)] public int maxCount = 4;
        public GameObject prefab;

        [Tooltip("Ground area it occupies (x, z relative to its pivot); nothing else can be placed on it. " +
                 "Measured from the prefab by Setup while Auto Footprint is on.")]
        public Rect area = new Rect(-0.7f, -0.7f, 1.4f, 1.4f);
        public bool autoFootprint = true;
    }
}
