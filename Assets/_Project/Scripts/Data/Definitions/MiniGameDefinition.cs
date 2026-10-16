using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Which game a MiniGameDefinition plays; each kind reads the MiniGameLevel fields it needs.</summary>
    public enum MiniGameKind
    {
        DonutRush = 0,
        TowelRun = 1,
        PerfectPour = 2,
        SurfDodge = 3,
        SkyBuilder = 4,
    }

    /// <summary>One level's difficulty. Fields a game does not use are ignored (see the tooltips).</summary>
    [Serializable]
    public struct MiniGameLevel
    {
        [Tooltip("What to reach to pass: orders served, towels handed out, drinks poured, rows of obstacles passed or floors built.")]
        [Min(1)] public int target;
        [Tooltip("Seconds to reach the target (0 = no limit).")]
        [Min(0f)] public float timeLimit;
        [Tooltip("Misses that end the level.")]
        [Min(1)] public int lives;
        [Tooltip("Drinks: share of the glass filled per second. Surf: obstacle speed (screen heights per second). Sky builder: slide speed (full slides per second).")]
        [Min(0f)] public float speed;
        [Tooltip("Seconds between arrivals: towel visitors, surf obstacle rows.")]
        [Min(0.1f)] public float pace;
        [Tooltip("Seconds something waits: a donut customer, a towel visitor.")]
        [Min(0.1f)] public float patience;
        [Tooltip("Size of the good zone: drinks band (share of the glass), sky builder drop window (share of half the slide).")]
        [Range(0.01f, 1f)] public float tolerance;
        [Tooltip("Donuts: icings to pick from (max 6). Towels: sunbeds (max 9). Surf: lanes (2 to 5).")]
        [Min(1)] public int choices;
    }

    /// <summary>
    /// A stand's mini-game: its kind, texts, levels from easy to hard, and the permanent sales bonus each passed
    /// level gives the stand's product (also one more decoration piece on the stand per level).
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Mini-game", fileName = "MiniGame")]
    public sealed class MiniGameDefinition : ScriptableObject
    {
        public string id;
        public MiniGameKind kind;
        [Tooltip("The stand that offers this game and gets its rewards.")]
        public ProductDefinition product;
        public string title = "Mini-game";
        [TextArea(2, 4)] public string howToPlay = string.Empty;
        [Tooltip("Permanent sales bonus for the product per level passed (0.1 = +10%).")]
        [Min(0f)] public float incomeBonusPerLevel = 0.1f;
        public MiniGameLevel[] levels = new MiniGameLevel[0];

        public int LevelCount => levels != null ? levels.Length : 0;
    }
}
