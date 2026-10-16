using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Rules for moving buildings and decorations (city-builder style drag and drop).</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Placement", fileName = "PlacementConfig")]
    public sealed class PlacementConfig : ScriptableObject
    {
        [Tooltip("Dragged objects snap to this grid (world units).")]
        [Min(0.05f)] public float gridSize = 0.5f;
        [Tooltip("Minimum empty gap kept between two objects.")]
        [Min(0f)] public float gap = 0.05f;
        [Tooltip("Area (x, z, width, depth) objects must stay inside: dry beach and grass, not the surf. " +
                 "Buildings may overhang the coast a little because the ground is levelled under them.")]
        public Rect buildArea = new Rect(-11.8f, -14.8f, 23.6f, 21.3f);
        [Tooltip("Decorations sit on the natural ground: its height at their centre must be at least this " +
                 "far above the water.")]
        public float minHeightAboveWater = 0f;
        [Tooltip("Space kept free in front of the main building's stands for visitor queues.")]
        [Min(0f)] public float mainFrontClearance = 4.2f;
        [Tooltip("How far to look for a free spot when a default position is taken.")]
        [Min(1f)] public float searchRadius = 14f;

        [Header("Feel")]
        [Tooltip("How high a picked-up object floats while dragged.")]
        [Min(0f)] public float liftHeight = 0.6f;
        [Tooltip("Dragging within this fraction of the screen edge scrolls the camera.")]
        [Range(0f, 0.3f)] public float edgePanZone = 0.08f;
        [Tooltip("Edge scroll speed in screen pixels per second.")]
        [Min(0f)] public float edgePanSpeed = 900f;
    }
}
