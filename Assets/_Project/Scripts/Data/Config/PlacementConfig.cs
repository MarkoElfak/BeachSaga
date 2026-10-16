using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>
    /// Rules for moving buildings, stands and decorations (city-builder style drag and drop). The buildable
    /// area grows with the main building: see TierDefinition.buildArea.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Placement", fileName = "PlacementConfig")]
    public sealed class PlacementConfig : ScriptableObject
    {
        [Tooltip("Dragged objects snap to this grid (world units).")]
        [Min(0.05f)] public float gridSize = 0.5f;
        [Tooltip("Minimum empty gap kept between two objects.")]
        [Min(0f)] public float gap = 0.05f;
        [Tooltip("Decorations sit on the natural ground: its height at their centre must be at least this " +
                 "far above the water.")]
        public float minHeightAboveWater = 0f;
        [Tooltip("Width of the strip kept free in front of every stand for its queue (its length comes from " +
                 "EconomyConfig.maxStandQueue x VisitorConfig.queueSpacing).")]
        [Min(0f)] public float queueWidth = 1f;
        [Tooltip("How far to look for a free spot when a default position is taken.")]
        [Min(1f)] public float searchRadius = 18f;

        [Header("Feel")]
        [Tooltip("How high a picked-up object floats while dragged.")]
        [Min(0f)] public float liftHeight = 0.6f;
        [Tooltip("Dragging within this fraction of the screen edge scrolls the camera.")]
        [Range(0f, 0.3f)] public float edgePanZone = 0.08f;
        [Tooltip("Edge scroll speed in screen pixels per second.")]
        [Min(0f)] public float edgePanSpeed = 900f;
    }
}
