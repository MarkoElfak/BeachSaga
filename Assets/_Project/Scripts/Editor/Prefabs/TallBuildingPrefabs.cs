using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Tier 4 and 5 buildings: a three-storey Hotel and a terraced Resort with a rooftop pool.</summary>
    internal static class TallBuildingPrefabs
    {
        private const float FloorHeight = 1.3f;

        public static GameObject BuildHotel(PaletteDefinition p)
        {
            var root = new GameObject("Hotel");
            Transform t = root.transform;
            Material plaster = p.GetMaterial("plaster");
            const float front = -1.25f;

            for (int floor = 0; floor < 3; floor++)
            {
                float y = floor * FloorHeight;
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Floor{floor}", new Vector3(0f, y + FloorHeight * 0.5f, 0.55f), new Vector3(4.3f, FloorHeight, 3.6f), plaster);
                Glazing(t, $"Glass{floor}", y + 0.7f, front, 3.7f, floor == 0 ? 0.9f : 0.6f, p);
                if (floor > 0) Balcony(t, $"Balcony{floor}", y + 0.12f, front, 3.9f, p);
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Roof", new Vector3(0f, 3 * FloorHeight + 0.08f, 0.55f), new Vector3(4.5f, 0.16f, 3.8f), p.GetMaterial("stone"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Canopy", new Vector3(0f, 1.15f, front - 0.45f), new Vector3(1.8f, 0.08f, 0.9f), p.GetMaterial("awning"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "SignBoard", new Vector3(0f, 3 * FloorHeight + 0.5f, front + 0.2f), new Vector3(2.2f, 0.6f, 0.1f), p.GetMaterial("roofBlue"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "SignStripe", new Vector3(0f, 3 * FloorHeight + 0.5f, front + 0.14f), new Vector3(1.8f, 0.12f, 0.02f), p.GetMaterial("coin"));
            RooftopUmbrella(t, new Vector3(1.4f, 3 * FloorHeight + 0.16f, 1.6f), p);
            RooftopUmbrella(t, new Vector3(-1.4f, 3 * FloorHeight + 0.16f, 1.6f), p);
            PrimitiveKit.AddBox(root, new Vector3(0f, 2f, 0.55f), new Vector3(4.3f, 4f, 3.6f));
            BuildingPrefabs.AddView(root, flagAnchor: new Vector3(-1.9f, 3 * FloorHeight + 0.16f, 2.2f)); // back roof corner
            return root;
        }

        public static GameObject BuildResort(PaletteDefinition p)
        {
            var root = new GameObject("Resort");
            Transform t = root.transform;
            Material plaster = p.GetMaterial("plaster");

            // Three stepped terraces, each smaller and set back.
            Vector3[] sizes = { new Vector3(4.4f, 1.5f, 3.8f), new Vector3(3.6f, 1.4f, 2.8f), new Vector3(2.6f, 1.2f, 1.9f) };
            float y = 0f, z = 0.6f, front = -1.3f;
            for (int i = 0; i < sizes.Length; i++)
            {
                Vector3 size = sizes[i];
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Terrace{i}", new Vector3(0f, y + size.y * 0.5f, z), size, plaster);
                Glazing(t, $"Glass{i}", y + size.y * 0.55f, z - size.z * 0.5f, size.x - 0.6f, size.y * 0.45f, p);
                if (i < sizes.Length - 1)
                {
                    PrimitiveKit.Part(t, PrimitiveType.Cube, $"Planter{i}", new Vector3(0f, y + size.y + 0.15f, z - size.z * 0.5f + 0.2f), new Vector3(size.x - 0.2f, 0.3f, 0.3f), p.GetMaterial("hedge"));
                }

                y += size.y;
                z += 0.45f;
            }

            // Rooftop pool with a gold rim and loungers.
            PrimitiveKit.Part(t, PrimitiveType.Cube, "PoolRim", new Vector3(0f, y + 0.08f, z - 0.45f), new Vector3(2.4f, 0.16f, 1.7f), p.GetMaterial("coin"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Pool", new Vector3(0f, y + 0.12f, z - 0.45f), new Vector3(2f, 0.12f, 1.3f), p.GetMaterial("pool"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Entrance", new Vector3(0f, 0.6f, front - 0.6f), new Vector3(1.6f, 1.2f, 0.12f), p.GetMaterial("glass"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Portico", new Vector3(0f, 1.25f, front - 0.75f), new Vector3(2.4f, 0.12f, 1.1f), p.GetMaterial("stone"));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "ColumnL", new Vector3(-1f, 0.62f, front - 1.15f), new Vector3(0.14f, 0.62f, 0.14f), plaster);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "ColumnR", new Vector3(1f, 0.62f, front - 1.15f), new Vector3(0.14f, 0.62f, 0.14f), plaster);
            PrimitiveKit.AddBox(root, new Vector3(0f, 2.1f, 0.8f), new Vector3(4.4f, 4.2f, 4.2f));
            BuildingPrefabs.AddView(root, flagAnchor: new Vector3(1.55f, 2.9f, 1.9f)); // middle terrace, beside the pool
            return root;
        }

        // A band of glass with thin mullions across the front face.
        private static void Glazing(Transform parent, string name, float centreY, float frontZ, float width, float height, PaletteDefinition p)
        {
            PrimitiveKit.Part(parent, PrimitiveType.Cube, name, new Vector3(0f, centreY, frontZ - 0.02f), new Vector3(width, height, 0.05f), p.GetMaterial("glass"));
            int mullions = Mathf.Max(2, Mathf.RoundToInt(width / 0.9f));
            for (int i = 0; i <= mullions; i++)
            {
                float x = -width * 0.5f + width * i / mullions;
                PrimitiveKit.Part(parent, PrimitiveType.Cube, $"{name}Mullion{i}", new Vector3(x, centreY, frontZ - 0.05f), new Vector3(0.05f, height, 0.04f), p.GetMaterial("plaster"));
            }
        }

        private static void Balcony(Transform parent, string name, float y, float frontZ, float width, PaletteDefinition p)
        {
            PrimitiveKit.Part(parent, PrimitiveType.Cube, name, new Vector3(0f, y, frontZ - 0.3f), new Vector3(width, 0.08f, 0.6f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(parent, PrimitiveType.Cube, name + "Rail", new Vector3(0f, y + 0.3f, frontZ - 0.58f), new Vector3(width, 0.45f, 0.03f), p.GetMaterial("glass"));
        }

        private static void RooftopUmbrella(Transform parent, Vector3 position, PaletteDefinition p)
        {
            PrimitiveKit.Part(parent, PrimitiveType.Cylinder, "UmbrellaPole", position + new Vector3(0f, 0.45f, 0f), new Vector3(0.04f, 0.45f, 0.04f), p.GetMaterial("metal"));
            PrimitiveKit.MeshPart(parent, MeshFactory.Umbrella, "Umbrella", position + new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.6f, 0.6f),
                new[] { p.GetMaterial("umbrellaWhite"), p.GetMaterial("umbrellaRed") });
        }
    }
}
