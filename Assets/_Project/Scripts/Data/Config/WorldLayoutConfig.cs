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
        public const int CurrentDataVersion = 4;

        [Tooltip("0 = never upgraded. Setup sets it to CurrentDataVersion.")]
        public int dataVersion;

        [Header("Island")]
        [Min(1f)] public float width = 40f;
        [Tooltip("World Z of the island's top (sea-side) edge.")]
        public float landTopZ = 10f;
        public BandSpec[] bands =
        {
            new BandSpec { name = "Shallows", paletteKey = "shallows", depth = 3f, elevation = -0.9f },
            new BandSpec { name = "FoamLine", paletteKey = "foam", depth = 0.3f, elevation = -0.12f },
            new BandSpec { name = "WetSand", paletteKey = "wetSand", depth = 3f, elevation = 0f },
            new BandSpec { name = "DrySand", paletteKey = "drySand", depth = 12f, elevation = 0.03f },
            new BandSpec { name = "Grass", paletteKey = "grass", depth = 9f, elevation = 0.06f },
        };

        [Tooltip("Palette key for the sea colour (camera background).")]
        public string seaPaletteKey = "sea";

        [Header("Palms")]
        public GameObject palmPrefab;
        [Tooltip("Index into bands where palms grow.")]
        [Min(0)] public int palmBandIndex = 4;
        [Min(0)] public int palmCount = 30;
        public int palmSeed = 7;
        [Min(0f)] public float palmEdgeMargin = 0.8f;
        [Tooltip("Palms keep this far from the footpath, and are hidden this close to any object.")]
        [Min(0f)] public float palmClearance = 1.7f;
        [Tooltip("Palms this far in front of (camera side of) a building or decoration are hidden too, so their crowns never cover it.")]
        [Min(0f)] public float palmFrontClearance = 2.5f;
        public Vector2 palmScaleRange = new Vector2(0.85f, 1.3f);

        [Header("Lots (x, z). Lot 0 holds the main building; the rest are neighbour house lots.")]
        public Vector2[] lotPositions =
        {
            new Vector2(0f, -2f),
            new Vector2(-5.5f, -10f), new Vector2(5.5f, -10f), new Vector2(-8f, -10f), new Vector2(8f, -10f),
            new Vector2(-10.5f, -10f), new Vector2(10.5f, -10f), new Vector2(-5.5f, -12.2f), new Vector2(5.5f, -12.2f),
            new Vector2(-8f, -12.2f), new Vector2(8f, -12.2f), new Vector2(-10.5f, -12.2f), new Vector2(10.5f, -12.2f),
            new Vector2(-5.5f, -14.4f), new Vector2(5.5f, -14.4f), new Vector2(-8f, -14.4f), new Vector2(8f, -14.4f),
            new Vector2(-10.5f, -14.4f), new Vector2(10.5f, -14.4f), new Vector2(-14f, -6.4f), new Vector2(14f, -6.4f),
        };

        [Tooltip("Ground area of a house (x, z relative to its lot). Measured from the house prefabs by Setup " +
                 "while Auto House Footprint is on. The main building's area comes from each TierDefinition.")]
        public Rect houseFootprint = new Rect(-1.1f, -1f, 2.2f, 2f);
        public bool autoHouseFootprint = true;

        [Header("Stands")]
        [Tooltip("Default stand row beside the main building: z of the stands relative to the lot.")]
        public float standRowZ = -0.6f;
        [Tooltip("Gap between the main building and its first stand, and between stands in the row.")]
        [Min(0f)] public float standGap = 0.4f;

        [Min(0f)] public float pathClearance = 0.5f;

        [Header("Visitors")]
        [Tooltip("Where visitors appear and leave (x, z), at the end of the footpath.")]
        public Vector2[] entryPoints = { new Vector2(-1.4f, -17.1f), new Vector2(0f, -17.1f), new Vector2(1.4f, -17.1f) };
        [Tooltip("Visitors pass this z on the way in and out; the footpath is kept clear up to here.")]
        public float beachAccessZ = -4.8f;
        [Tooltip("Area (x, z, width, depth) where visitors lay their towels.")]
        public Rect relaxArea = new Rect(-8.8f, 1.9f, 17.6f, 3f);
        [Min(0.5f)] public float relaxSpotSpacing = 1.15f;
        [Min(0f)] public float relaxSpotJitter = 0.3f;

        [Header("Decorations")]
        [Tooltip("Preferred spots for newly bought decorations (x, z); when all are taken the nearest free place is used.")]
        public Vector2[] decorationSpots =
        {
            new Vector2(-10.4f, 5f), new Vector2(10.4f, 5f), new Vector2(-10.4f, 2.6f), new Vector2(10.4f, 2.6f),
            new Vector2(-10.4f, 0.2f), new Vector2(10.4f, 0.2f),
        };
    }
}
