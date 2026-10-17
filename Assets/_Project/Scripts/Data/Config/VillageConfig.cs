using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>The village the player founds on first launch: name rules, name ideas and the welcome sign.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Village", fileName = "VillageConfig")]
    public sealed class VillageConfig : ScriptableObject
    {
        [Header("Name")]
        [Min(1)] public int minNameLength = 2;
        [Min(1)] public int maxNameLength = 20;
        [Tooltip("Ideas the dice button picks from.")]
        public string[] suggestedNames =
        {
            "Sunny Bay", "Coral Cove", "Palm Harbour", "Seashell Point", "Golden Sands", "Driftwood Bay",
            "Blue Lagoon", "Starfish Beach", "Sandy Hollow", "Breezy Shore", "Turtle Cove", "Pebble Bay",
        };

        [Header("Welcome sign")]
        [Tooltip("Where the sign stands (x, z): beside the footpath where visitors walk in.")]
        public Vector2 signPosition = new Vector2(3.6f, -17.2f);
        [Tooltip("Turn in degrees; 0 faces the camera.")]
        public float signYaw = -12f;
    }
}
