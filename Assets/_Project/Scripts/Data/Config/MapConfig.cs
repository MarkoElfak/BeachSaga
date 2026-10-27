using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>One piece of land that opens when the main building reaches a tier.</summary>
    [Serializable]
    public struct LandPiece
    {
        public string id;
        public string displayName;
        [Tooltip("Main-building tier that opens this piece (1 = open from the start).")]
        [Min(1)] public int unlockTier;
        [Tooltip("Buildable ground (x, z, width, depth).")]
        public Rect area;
        [Tooltip("A separate island in the sea, reached by a boardwalk. Mainland pieces join the land beside them, " +
                 "and every tier's open mainland together must form one rectangle.")]
        public bool island;
        [Tooltip("Where visitors lay their towels on this piece (width 0 = none).")]
        public Rect towelArea;
        [Tooltip("Where the 'Opens at ...' sign stands while the piece is under fog (x, z).")]
        public Vector2 signPosition;
        [Tooltip("Islands only: the boardwalk runs from the mainland (from) to the island (to), x z.")]
        public Vector2 boardwalkFrom;
        public Vector2 boardwalkTo;
    }

    /// <summary>
    /// The map: today's beach opens at the first tier and each later tier opens a quarter-size piece around it.
    /// Locked pieces sit under drifting fog with a sign; saved positions keep their coordinates.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Map", fileName = "MapConfig")]
    public sealed class MapConfig : ScriptableObject
    {
        /// <summary>Bumped when the default pieces change; Setup then resets older assets to the code defaults.</summary>
        public const int CurrentDataVersion = 1;

        [HideInInspector] public int dataVersion = CurrentDataVersion;

        public LandPiece[] pieces =
        {
            new LandPiece
            {
                id = "home_beach", displayName = "Home Beach", unlockTier = 1,
                area = Rect.MinMaxRect(-19.2f, -16.9f, 19.2f, 6.5f),
            },
            new LandPiece
            {
                id = "west_cove", displayName = "West Cove", unlockTier = 2,
                area = Rect.MinMaxRect(-28.8f, -16.9f, -19.2f, 6.5f),
                towelArea = Rect.MinMaxRect(-27.6f, 1.9f, -20.4f, 4.9f), signPosition = new Vector2(-21f, -1f),
            },
            new LandPiece
            {
                id = "east_point", displayName = "East Point", unlockTier = 3,
                area = Rect.MinMaxRect(19.2f, -16.9f, 28.8f, 6.5f),
                towelArea = Rect.MinMaxRect(20.4f, 1.9f, 27.6f, 4.9f), signPosition = new Vector2(21f, -1f),
            },
            new LandPiece
            {
                id = "hillside", displayName = "Hillside", unlockTier = 4,
                area = Rect.MinMaxRect(-28.8f, -20.8f, 28.8f, -16.9f), signPosition = new Vector2(-6.5f, -18.4f),
            },
            new LandPiece
            {
                id = "lagoon_islet", displayName = "Lagoon Islet", unlockTier = 5, island = true,
                area = Rect.MinMaxRect(-7.5f, 15.5f, 7.5f, 30.5f),
                towelArea = Rect.MinMaxRect(-5.5f, 26.6f, 5.5f, 29.6f), signPosition = new Vector2(3.4f, 16.6f),
                boardwalkFrom = new Vector2(0f, 5.2f), boardwalkTo = new Vector2(0f, 15.9f),
            },
        };

        [Header("Islands")]
        [Tooltip("Sand around an island's buildable area before its coast drops into the sea.")]
        [Min(0f)] public float islandBeach = 2.5f;
        [Tooltip("Island ground height (the mainland's dry sand is about 0.03).")]
        public float islandElevation = 0.08f;
        [Min(0.5f)] public float boardwalkWidth = 2f;
        [Tooltip("Deck height above the water.")]
        [Min(0f)] public float boardwalkHeight = 0.3f;
        [Tooltip("Planks and posts (created by Setup).")]
        public Material boardwalkMaterial;
        public Mesh boardwalkMesh;

        [Header("Fog over locked land")]
        [Tooltip("Created by Setup (BeachTycoon/Fog shader).")]
        public Material fogMaterial;
        [Tooltip("The fog thins out over this distance inside a piece's edge, so the land just beyond can be glimpsed.")]
        [Min(0.1f)] public float fogFadeInside = 2f;
        [Tooltip("...and spills this far past the edge.")]
        [Min(0f)] public float fogFadeOutside = 1f;
        public float fogHeight = 1.4f;
        [Min(1)] public int fogLayers = 2;
        public float fogLayerGap = 1f;
        [Min(0.5f)] public float fogCellSize = 1.5f;
        [Tooltip("Seconds for the fog to roll away when a piece opens.")]
        [Min(0.05f)] public float revealSeconds = 1.8f;
        [Tooltip("Sign shown on each locked piece (created by Setup).")]
        public GameObject signPrefab;
    }
}
