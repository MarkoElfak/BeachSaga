using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Global economy tuning. Every gameplay number that is not per product or per tier lives here.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Economy", fileName = "EconomyConfig")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [Min(0)] public long startingCoins;

        [Tooltip("Upgrade cost = base * growth ^ level.")]
        [Min(1f)] public float upgradeCostGrowth = 1.15f;

        [Tooltip("Extra income per stand level (0.10 = +10% per level).")]
        [Min(0f)] public float standLevelIncomeBonus = 0.10f;

        [Min(0.1f)] public float baseSpawnIntervalSeconds = 6f;
        [Range(0f, 1f)] public float offlineEfficiency = 0.5f;
        [Min(0f)] public float offlineCapHours = 8f;
        [Tooltip("Shorter absences pay nothing and show no popup.")]
        [Min(0f)] public float offlineMinSeconds = 60f;
        [Min(1f)] public float autosaveSeconds = 15f;

        [Tooltip("Orders a single stand can hold, including the one being served.")]
        [Min(1)] public int maxStandQueue = 5;

        [Min(1)] public int maxStandLevel = 25;
    }
}
