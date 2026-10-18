using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>A pretend friend whose island is generated locally until there is a real backend.</summary>
    [Serializable]
    public struct DemoIsland
    {
        public string ownerId;
        public string displayName;
        public string villageName;
        public string cultureId;
        [Min(1)] public int tier;
        [Min(0)] public int houses;
        [Min(0)] public int decorations;
        [Min(0f)] public float popularity;
    }

    /// <summary>Visiting other islands: the visit coin bonus, the simulated crowd and the demo friends.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Online", fileName = "OnlineConfig")]
    public sealed class OnlineConfig : ScriptableObject
    {
        [Tooltip("Name shown on the player's own island.")]
        public string defaultPlayerName = "You";

        [Header("Visit bonus (once per island per local day)")]
        [Tooltip("Coins = this many minutes of the player's current income...")]
        [Min(0f)] public float bonusIncomeMinutes = 2f;
        [Tooltip("...but never less than this.")]
        [Min(0)] public long bonusMinimumCoins = 50;
        [Min(0)] public int maxBonusesPerDay = 5;

        [Header("Visited island")]
        [Tooltip("Simulated visitors on a visited island (also capped by the island's tier).")]
        [Min(0)] public int maxVisitors = 18;
        [Min(0.1f)] public float visitorSpawnSeconds = 1.2f;
        [Tooltip("Seconds a visitor spends at a stand or on a towel (min, max).")]
        public Vector2 activitySecondsRange = new Vector2(3f, 9f);
        [Tooltip("Things a visitor does before leaving (min, max inclusive).")]
        public Vector2Int activitiesRange = new Vector2Int(2, 4);

        [Header("Mock friends")]
        public DemoIsland[] demoFriends =
        {
            new DemoIsland { ownerId = "demo-coral", displayName = "Coral", villageName = "Coral Cove", cultureId = "greece", tier = 2, houses = 1, decorations = 3, popularity = 0.6f },
            new DemoIsland { ownerId = "demo-kai", displayName = "Kai", villageName = "Palm Harbour", cultureId = "spain", tier = 3, houses = 3, decorations = 6, popularity = 1.2f },
            new DemoIsland { ownerId = "demo-marina", displayName = "Marina", villageName = "Marina Bay", cultureId = "italy", tier = 5, houses = 5, decorations = 10, popularity = 2.1f },
        };
    }
}
