using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Neighbour houses: small cottages in a few colour variants, each with a little garden.</summary>
    internal static class HousePrefabs
    {
        public static readonly (string name, string wall, string roof)[] Variants =
        {
            ("HouseSand", "houseA", "roof"),
            ("HouseMint", "houseB", "roofBlue"),
            ("HouseCoral", "houseC", "roofGreen"),
        };

        public static GameObject Build(int variant, PaletteDefinition p)
        {
            var (name, wall, roof) = Variants[variant];
            var root = new GameObject(name);
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Base", new Vector3(0f, 0.06f, 0.1f), new Vector3(2.1f, 0.12f, 1.7f), p.GetMaterial("stone"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Walls", new Vector3(0f, 0.72f, 0.1f), new Vector3(1.9f, 1.2f, 1.5f), p.GetMaterial(wall));
            PrimitiveKit.GableRoof(t, "Roof", new Vector3(0f, 1.32f, 0.1f), 2.3f, 1.8f, p.GetMaterial(roof));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Door", new Vector3(0.45f, 0.55f, -0.66f), new Vector3(0.42f, 0.8f, 0.04f), wood);
            BuildingPrefabs.Window(t, "Window", new Vector3(-0.4f, 0.8f, -0.66f), p, 0.5f, 0.42f);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Chimney", new Vector3(-0.55f, 1.85f, 0.35f), new Vector3(0.22f, 0.6f, 0.22f), p.GetMaterial("stone"));

            // Front garden: a hedge on each side and a path to the door.
            PrimitiveKit.Part(t, PrimitiveType.Cube, "HedgeL", new Vector3(-0.75f, 0.18f, -0.92f), new Vector3(0.6f, 0.32f, 0.28f), p.GetMaterial("hedge"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "HedgeR", new Vector3(0.95f, 0.18f, -0.92f), new Vector3(0.2f, 0.32f, 0.28f), p.GetMaterial("hedge"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Path", new Vector3(0.45f, 0.02f, -0.95f), new Vector3(0.4f, 0.03f, 0.3f), p.GetMaterial("stone"));

            PrimitiveKit.AddBox(root, new Vector3(0f, 1f, 0.1f), new Vector3(2.1f, 2f, 1.7f));
            BuildingPrefabs.AddView(root, new Vector3(0.45f, 0.8f, -1.35f));
            return root;
        }
    }
}
