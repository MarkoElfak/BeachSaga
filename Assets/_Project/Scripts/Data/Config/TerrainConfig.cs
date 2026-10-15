using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>
    /// Procedural island look: heights come from the WorldLayoutConfig bands (elevation at each band's centre),
    /// with dunes, a noisy coastline and flattened lots. Colours come from the palette.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Terrain", fileName = "TerrainConfig")]
    public sealed class TerrainConfig : ScriptableObject
    {
        [Header("Materials (created by Setup)")]
        public Material terrainMaterial;
        public Material waterMaterial;
        public Material rockMaterial;
        public Material grassTuftMaterial;

        [Header("Mesh")]
        [Min(0.1f)] public float meshCellSize = 0.5f;
        [Tooltip("Extra terrain beyond the island sides, where the coast falls into the sea.")]
        [Min(0f)] public float sideMargin = 9f;
        [Min(0f)] public float seaMargin = 12f;
        [Min(0f)] public float backMargin = 7f;

        [Header("Shape")]
        public float waterLevel = -0.05f;
        public float seaFloorHeight = -2.4f;
        [Tooltip("Distance over which the land drops to the sea floor past the outermost band.")]
        [Min(0.1f)] public float coastFalloff = 5f;
        [Min(0f)] public float coastNoiseAmplitude = 1f;
        [Min(0.001f)] public float coastNoiseScale = 0.09f;
        [Min(0f)] public float duneAmplitude = 0.12f;
        [Min(0.001f)] public float duneScale = 0.16f;
        [Tooltip("Lots are flattened to height 0 within their footprint plus this margin.")]
        [Min(0f)] public float lotFlattenMargin = 1.2f;
        public int noiseSeed = 11;

        [Header("Baked texture")]
        [Min(64)] public int textureSize = 512;
        [Tooltip("Width of the colour blend between neighbouring bands.")]
        [Min(0.01f)] public float bandBlend = 1.4f;
        [Range(0f, 0.3f)] public float colorNoise = 0.07f;
        [Tooltip("Land this close above the water reads as wet sand.")]
        [Min(0f)] public float wetHeight = 0.16f;
        public Color underwaterTint = new Color(0.55f, 0.78f, 0.74f);
        public Color grassVariation = new Color(0.62f, 0.74f, 0.4f);
        public Color pathColor = new Color(0.86f, 0.76f, 0.56f);
        [Tooltip("Sandy footpath from the entry points to the beach (x, z, width, depth).")]
        public Rect path = new Rect(-2.4f, -15f, 4.8f, 7f);
        [Range(0f, 1f)] public float drySmoothness = 0.08f;
        [Range(0f, 1f)] public float wetSmoothness = 0.55f;
        [Range(0f, 1f)] public float grassSmoothness = 0.04f;
        [Min(0f)] public float normalStrength = 2.5f;

        [Header("Water")]
        [Tooltip("Water depth at which the shore foam factor reaches zero.")]
        [Min(0.01f)] public float shoreDepthRange = 0.9f;
        [Min(0.01f)] public float deepDepthRange = 2.2f;
        [Min(0.1f)] public float waterCellSize = 1f;
        [Tooltip("Flat water continues this far beyond the terrain so the horizon never shows a gap.")]
        [Min(0f)] public float waterExtent = 160f;

        [Header("Scatter (GPU instanced)")]
        [Min(0)] public int rockCount = 26;
        [Min(0)] public int grassTuftCount = 220;
        public Vector2 rockScaleRange = new Vector2(0.25f, 0.8f);
        public Vector2 tuftScaleRange = new Vector2(0.6f, 1.3f);
    }
}
