using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>How a flag is drawn: equal or weighted stripes, plus an optional top-left canton with a cross.</summary>
    [Serializable]
    public sealed class FlagDesign
    {
        [Tooltip("Off: stripes run top to bottom. On: left to right.")]
        public bool vertical;
        public Color[] stripes = { Color.white };
        [Tooltip("Relative stripe sizes, one per stripe. Empty = all equal.")]
        public float[] stripeWeights = new float[0];

        [Header("Canton (top-left box)")]
        public bool canton;
        [Tooltip("Share of the flag's width (x) and height (y).")]
        public Vector2 cantonSize = new Vector2(0.37f, 0.556f);
        public Color cantonColor = Color.blue;
        public Color crossColor = Color.white;
        [Tooltip("Cross bar thickness as a share of the canton's height. 0 = no cross.")]
        [Range(0f, 0.5f)] public float crossWidth = 0.2f;
    }

    /// <summary>Repaints one palette colour (for example walls or roofs) on this culture's buildings.</summary>
    [Serializable]
    public struct PaletteRecolor
    {
        public string paletteKey;
        public Color color;
    }

    /// <summary>A culture's own version of a shared prefab.</summary>
    [Serializable]
    public struct PrefabSwap
    {
        public GameObject original;
        public GameObject replacement;
    }

    /// <summary>
    /// A culture the player picks once, when founding the village. Adding a culture is adding one of these assets
    /// to the Cultures folder and running Setup; no code changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Culture", fileName = "Culture")]
    public sealed class CultureDefinition : ScriptableObject
    {
        [Tooltip("Stable id stored in saves and snapshots. Never change it once players have picked it.")]
        public string id;
        public string displayName;
        [Tooltip("Position on the start screen, lowest first.")]
        public int order;
        public FlagDesign flag = new FlagDesign();
        [Tooltip("Palette colours repainted on this culture's buildings and houses, such as walls and roofs.")]
        public PaletteRecolor[] recolors = new PaletteRecolor[0];
        [Tooltip("Ideas for the Idea button once this culture is picked.")]
        public string[] villageNames = new string[0];
        [Tooltip("Culture versions of buildings, stands and decorations. Anything not listed uses the shared prefab.")]
        public PrefabSwap[] prefabSwaps = new PrefabSwap[0];
        [Tooltip("The hero picked with this culture: they run the beach and work at a stand.")]
        public CharacterDefinition character;

        /// <summary>This culture's version of a prefab, or the prefab itself when it has none.</summary>
        public GameObject Resolve(GameObject prefab)
        {
            if (prefab == null || prefabSwaps == null) return prefab;
            for (int i = 0; i < prefabSwaps.Length; i++)
            {
                if (prefabSwaps[i].original == prefab && prefabSwaps[i].replacement != null) return prefabSwaps[i].replacement;
            }

            return prefab;
        }
    }
}
