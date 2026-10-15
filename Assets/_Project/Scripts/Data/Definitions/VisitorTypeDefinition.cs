using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>A kind of beach visitor. Art is swapped through prefab.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Visitor Type", fileName = "VisitorType")]
    public sealed class VisitorTypeDefinition : ScriptableObject
    {
        public string id;
        [Min(0f)] public float spawnWeight = 1f;
        [Min(0f)] public float spendMultiplier = 1f;
        [Min(0f)] public float patienceSeconds = 20f;
        public ProductDefinition[] preferredProducts;
        [Min(1)] public int minTier = 1;

        [Tooltip("Prefab with a VisitorView component.")]
        public GameObject prefab;
    }
}
