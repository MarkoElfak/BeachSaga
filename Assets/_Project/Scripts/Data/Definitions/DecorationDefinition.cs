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
    }
}
