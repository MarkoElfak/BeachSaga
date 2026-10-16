using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>One horizontal strip of the island, listed from the top of the screen (sea side) down.</summary>
    [Serializable]
    public struct BandSpec
    {
        public string name;
        [Tooltip("Key into the PaletteDefinition (used for land colour).")]
        public string paletteKey;
        [Min(0.01f)] public float depth;
        [Tooltip("Terrain height at the band's centre line; heights blend smoothly between bands.")]
        public float elevation;
    }

    /// <summary>Island layout: bands, palms, lots, visitor routes and decoration spots.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/World Layout", fileName = "WorldLayoutConfig")]
    public sealed class WorldLayoutConfig : ScriptableObject
    {
        /// <summary>Setup resets older assets to these defaults when the layout changes between milestones.</summary>
        public const int CurrentDataVersion = 3;

        [Tooltip("0 = never upgraded. Setup sets it to CurrentDataVersion.")]
        public int dataVersion;

        [Header("Island")]
        [Min(1f)] public float width = 24f;
        [Tooltip("World Z of the island's top (sea-side) edge.")]
        public float landTopZ = 10f;
        public BandSpec[] bands =
        {
            new BandSpec { name = "Shallows", paletteKey = "shallows", depth = 3f, elevation = -0.9f },
            new BandSpec { name = "FoamLine", paletteKey = "foam", depth = 0.3f, elevation = -0.12f },
            new BandSpec { name = "WetSand", paletteKey = "wetSand", depth = 3f, elevation = 0f },
            new BandSpec { name = "DrySand", paletteKey = "drySand", depth = 12f, elevation = 0.03f },
            new BandSpec { name = "Grass", paletteKey = "grass", depth = 6f, elevation = 0.06f },
        };

        [Tooltip("Palette key for the sea colour (camera background).")]
        public string seaPaletteKey = "sea";

        [Header("Palms")]
        public GameObject palmPrefab;
        [Tooltip("Index into bands where palms grow.")]
        [Min(0)] public int palmBandIndex = 4;
        [Min(0)] public int palmCount = 16;
        public int palmSeed = 7;
        [Min(0f)] public float palmEdgeMargin = 0.8f;
        [Tooltip("Palms keep this far from lot centres and the footpath.")]
        [Min(0f)] public float palmClearance = 1.7f;
        [Tooltip("Palms this far in front of (camera side of) a building or decoration are hidden too, so their crowns never cover it.")]
        [Min(0f)] public float palmFrontClearance = 2.5f;
        public Vector2 palmScaleRange = new Vector2(0.85f, 1.3f);

        [Header("Lots (x, z). Lot 0 holds the main building; the rest are neighbour house lots.")]
        public Vector2[] lotPositions =
        {
            new Vector2(0f, -2f),
            new Vector2(-5.5f, -9.4f), new Vector2(5.5f, -9.4f), new Vector2(-8f, -9.4f), new Vector2(8f, -9.4f),
            new Vector2(-10.5f, -9.4f), new Vector2(10.5f, -9.4f), new Vector2(-5.5f, -11.5f), new Vector2(5.5f, -11.5f),
            new Vector2(-8f, -11.5f), new Vector2(8f, -11.5f), new Vector2(-10.5f, -11.5f), new Vector2(10.5f, -11.5f),
            new Vector2(-5.5f, -13.6f), new Vector2(5.5f, -13.6f), new Vector2(-8f, -13.6f), new Vector2(8f, -13.6f),
            new Vector2(-10.5f, -13.6f), new Vector2(10.5f, -13.6f), new Vector2(-10.5f, -6.4f), new Vector2(10.5f, -6.4f),
        };

        [Tooltip("Area (relative to the lot, x/z min and size) the main building and its stands block for walkers.")]
        public Rect mainFootprint = new Rect(-6.6f, -1.6f, 15.4f, 5.2f);
        public Rect houseFootprint = new Rect(-1.1f, -1f, 2.2f, 2f);
        [Min(0f)] public float pathClearance = 0.5f;

        [Header("Visitors")]
        [Tooltip("Where visitors appear and leave (x, z), at the end of the footpath.")]
        public Vector2[] entryPoints = { new Vector2(-1.4f, -14.1f), new Vector2(0f, -14.1f), new Vector2(1.4f, -14.1f) };
        [Tooltip("Visitors pass this z (just in front of the stands) on the way in and out.")]
        public float beachAccessZ = -4.8f;
        [Tooltip("Area (x, z, width, depth) where visitors lay their towels.")]
        public Rect relaxArea = new Rect(-9.6f, 1.9f, 19.2f, 3f);
        [Min(0.5f)] public float relaxSpotSpacing = 1.15f;
        [Min(0f)] public float relaxSpotJitter = 0.3f;

        [Header("Decorations")]
        public Vector2[] decorationSpots =
        {
            new Vector2(-10.3f, 5f), new Vector2(10.3f, 5f), new Vector2(-10.3f, 2.6f), new Vector2(10.3f, 2.6f),
            new Vector2(-10.3f, 0.2f), new Vector2(10.3f, 0.2f), new Vector2(-10.2f, -2.6f), new Vector2(10.2f, -2.6f),
            new Vector2(-3.4f, -8.9f), new Vector2(3.4f, -8.9f),
        };
    }
}
