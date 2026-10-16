using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Repeat visitors who move into houses on free lots, pay rent and hand out quests.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Neighbours", fileName = "NeighbourConfig")]
    public sealed class NeighbourConfig : ScriptableObject
    {
        [Tooltip("Purchases in one visit before a visitor may ask to stay.")]
        [Min(1)] public int minPurchasesToAsk = 3;
        [Range(0f, 1f)] public float askChance = 0.1f;
        [Tooltip("An unanswered request disappears after this long.")]
        [Min(1f)] public float requestTimeoutSeconds = 25f;

        [Header("Rent")]
        [Min(0)] public long rentPerMinute = 20;
        [Tooltip("Rent drops as a coin pickup at the house this often.")]
        [Min(1f)] public float rentIntervalSeconds = 60f;

        [Header("Houses")]
        [Tooltip("House prefabs (BuildingView). A neighbour gets one at random.")]
        public GameObject[] housePrefabs;
        [Min(0f)] public float houseBuildSeconds = 3f;

        public string[] names = { "Mia", "Leo", "Ana", "Noah", "Iva", "Luka", "Sara", "Marko", "Ema", "Teo" };
    }
}
