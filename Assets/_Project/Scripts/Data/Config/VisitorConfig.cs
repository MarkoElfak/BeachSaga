using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Visitor behaviour and popularity tuning.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Visitors", fileName = "VisitorConfig")]
    public sealed class VisitorConfig : ScriptableObject
    {
        [Header("Movement")]
        [Min(0.1f)] public float walkSpeed = 2.4f;
        [Tooltip("Distance at which a waypoint counts as reached.")]
        [Min(0.01f)] public float arriveDistance = 0.1f;
        [Tooltip("Gap between visitors standing in a stand's queue.")]
        [Min(0.1f)] public float queueSpacing = 0.9f;
        [Tooltip("Visitors per queue line; the next ones start a new line beside it.")]
        [Min(1)] public int queuePerLine = 3;
        [Tooltip("Queue lines side by side in front of a stand.")]
        [Min(1)] public int queueLines = 2;
        [Tooltip("Sideways distance between two queue lines.")]
        [Min(0.1f)] public float queueLineSpacing = 0.8f;

        [Header("Behaviour")]
        [Tooltip("Seconds spent relaxing on the towel between purchases (min, max).")]
        public Vector2 relaxSecondsRange = new Vector2(3f, 8f);
        [Tooltip("Purchases a visitor makes before going home (min, max inclusive).")]
        public Vector2Int purchasesRange = new Vector2Int(1, 4);
        [Tooltip("Relax time before trying again when every stand queue is full.")]
        [Min(0.1f)] public float retryQueueSeconds = 3f;
        [Tooltip("Average length of one visit (arrive to leave). Used only to estimate offline income.")]
        [Min(1f)] public float estimatedVisitSeconds = 80f;
        public int randomSeed = 1234;

        [Header("Pool")]
        [Min(1)] public int poolPrewarm = 8;
        [Tooltip("Hard cap on simultaneous visitors, whatever the tier allows.")]
        [Min(1)] public int poolMaxSize = 120;

        [Header("Popularity")]
        [Min(0f)] public float popularityPerStand = 0.1f;
        [Min(0f)] public float popularityPerDecoration = 0.05f;
        [Tooltip("Popularity lost each time a visitor leaves a queue unserved.")]
        [Min(0f)] public float unservedPenalty = 0.05f;
        [Min(0f)] public float maxUnservedPenalty = 1f;
        [Tooltip("How fast the unserved penalty recovers, per second.")]
        [Min(0f)] public float penaltyDecayPerSecond = 0.004f;
    }
}
