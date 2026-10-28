using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The chapel in each country's style, all on about the same ground: a white Greek chapel with a blue dome, a
    /// Spanish stone church with a bell gable (the shared one) and an Italian church with its bell tower.
    /// </summary>
    internal static class ChapelPrefabs
    {
        private static readonly Vector3 Box = new Vector3(2.8f, 3.6f, 3f);
        private const float FrontZ = -1.1f;
        private const float BarHeight = 4.4f;

        public static GameObject BuildGreek(PaletteDefinition p)
        {
            var root = new GameObject("ChapelGreek");
            Transform t = root.transform;
            Material white = p.GetMaterial("plaster"), blue = p.GetMaterial("roofBlue");
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Body", new Vector3(0f, 0.8f, 0.2f), new Vector3(2f, 1.6f, 2.6f), white);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "Drum", new Vector3(0f, 1.85f, 0.2f), new Vector3(1f, 0.25f, 1f), white);
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Dome", new Vector3(0f, 2.1f, 0.2f), new Vector3(1.1f, 1f, 1.1f), blue);
            Cross(t, new Vector3(0f, 2.85f, 0.2f), p.GetMaterial("coin"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "BellWall", new Vector3(0f, 1.95f, -0.95f), new Vector3(0.9f, 0.7f, 0.15f), white);
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Bell", new Vector3(0f, 1.9f, -1.04f), new Vector3(0.24f, 0.24f, 0.1f), p.GetMaterial("coin"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Door", new Vector3(0f, 0.45f, -1.11f), new Vector3(0.55f, 0.9f, 0.03f), blue);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Step", new Vector3(0f, 0.06f, -1.3f), new Vector3(1f, 0.12f, 0.4f), white);
            return VillagePrefabs.Finish(root, p, Box, FrontZ, BarHeight, false);
        }

        public static GameObject BuildSpanish(PaletteDefinition p)
        {
            var root = new GameObject("ChapelSpanish");
            Transform t = root.transform;
            Material stone = p.GetMaterial("stone");
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Body", new Vector3(0f, 0.9f, 0.2f), new Vector3(2f, 1.8f, 2.6f), stone);
            PrimitiveKit.GableRoof(t, "Roof", new Vector3(0f, 1.8f, 0.2f), 2.2f, 2.8f, p.GetMaterial("roof"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "BellGable", new Vector3(0f, 2.4f, -0.98f), new Vector3(1.2f, 1.2f, 0.25f), stone);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "GableTop", new Vector3(0f, 3.05f, -0.98f), new Vector3(0.4f, 0.4f, 0.25f), stone, new Vector3(0f, 0f, 45f));
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Opening{side}", new Vector3(0.28f * side, 2.45f, -1.1f), new Vector3(0.32f, 0.45f, 0.02f), p.GetMaterial("shorts"));
                PrimitiveKit.Part(t, PrimitiveType.Sphere, $"Bell{side}", new Vector3(0.28f * side, 2.4f, -1.14f), new Vector3(0.22f, 0.24f, 0.08f), p.GetMaterial("coin"));
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Door", new Vector3(0f, 0.5f, -1.11f), new Vector3(0.6f, 1f, 0.03f), p.GetMaterial("wood"));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "RoseWindow", new Vector3(0f, 1.4f, -1.11f), new Vector3(0.4f, 0.02f, 0.4f), p.GetMaterial("window"),
                new Vector3(90f, 0f, 0f));
            return VillagePrefabs.Finish(root, p, Box, FrontZ, BarHeight, false);
        }

        public static GameObject BuildItalian(PaletteDefinition p)
        {
            var root = new GameObject("ChapelItalian");
            Transform t = root.transform;
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Body", new Vector3(-0.35f, 0.8f, 0.3f), new Vector3(1.8f, 1.6f, 2.4f), p.GetMaterial("houseC"));
            PrimitiveKit.GableRoof(t, "Roof", new Vector3(-0.35f, 1.6f, 0.3f), 2f, 2.6f, p.GetMaterial("roof"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Tower", new Vector3(0.95f, 1.8f, 0.6f), new Vector3(0.8f, 3.6f, 0.8f), p.GetMaterial("castle"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Belfry", new Vector3(0.95f, 3.1f, 0.19f), new Vector3(0.45f, 0.5f, 0.02f), p.GetMaterial("shorts"));
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Bell", new Vector3(0.95f, 3.05f, 0.17f), new Vector3(0.28f, 0.3f, 0.08f), p.GetMaterial("coin"));
            PrimitiveKit.MeshPart(t, MeshFactory.Pyramid, "TowerRoof", new Vector3(0.95f, 3.6f, 0.6f), new Vector3(0.6f, 0.7f, 0.6f), p.GetMaterial("roof"),
                new Vector3(0f, 45f, 0f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Door", new Vector3(-0.35f, 0.5f, -0.91f), new Vector3(0.6f, 1f, 0.03f), p.GetMaterial("wood"));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "RoundWindow", new Vector3(-0.35f, 1.3f, -0.91f), new Vector3(0.35f, 0.02f, 0.35f), p.GetMaterial("window"),
                new Vector3(90f, 0f, 0f));
            return VillagePrefabs.Finish(root, p, Box, FrontZ, BarHeight, false);
        }

        private static void Cross(Transform parent, Vector3 at, Material material)
        {
            PrimitiveKit.Part(parent, PrimitiveType.Cube, "CrossV", at, new Vector3(0.06f, 0.4f, 0.06f), material);
            PrimitiveKit.Part(parent, PrimitiveType.Cube, "CrossH", at + new Vector3(0f, 0.06f, 0f), new Vector3(0.24f, 0.06f, 0.06f), material);
        }
    }
}
