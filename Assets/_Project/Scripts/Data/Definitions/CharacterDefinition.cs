using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>What the character wears on the head (drawn on the prefab and the portrait).</summary>
    public enum CharacterHeadwear
    {
        None = 0,
        Cap,
        ChefHat,
        Flower,
    }

    /// <summary>
    /// The hero who runs the beach: one per culture, picked with it on the start screen. They walk the beach and
    /// work at one stand, which then serves faster. Looks are palette keys; Setup builds the prefab from them.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Character", fileName = "Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Stable id. Never change it once players have picked it.")]
        public string id;
        public string displayName;
        [Tooltip("One short line under the name, such as \"Fisherman from Naxos\".")]
        public string tagline;
        [Tooltip("What the character does for the beach, shown on their card.")]
        [TextArea] public string ability;

        [Tooltip("Extra serving speed at the stand they work at (0.25 = 25% faster).")]
        [Range(0f, 2f)] public float serveSpeedBonus = 0.25f;
        [Tooltip("Walking speed in metres per second.")]
        [Min(0.1f)] public float walkSpeed = 2.8f;

        [Header("Look (palette keys)")]
        public string shirt = "shirtBlue";
        public string shorts = "shorts";
        public string skin = "skin";
        public string hair = "hairDark";
        public bool longHair;
        public CharacterHeadwear headwear;
        public string headwearColor = "shorts";
        [Tooltip("Behind the face on portraits.")]
        public Color portraitBackground = new Color(0.55f, 0.8f, 0.95f);

        [Tooltip("Built by Setup from the look above.")]
        public GameObject prefab;
    }
}
