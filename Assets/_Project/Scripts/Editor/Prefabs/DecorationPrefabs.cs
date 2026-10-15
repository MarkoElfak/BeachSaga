using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Beach decorations bought from the shop: umbrella set, lifeguard tower, sandcastle, volleyball, deck chairs.</summary>
    internal static class DecorationPrefabs
    {
        public static GameObject BuildUmbrella(PaletteDefinition p)
        {
            var root = new GameObject("BeachUmbrella");
            Transform t = root.transform;
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1f, 0f), new Vector3(0.05f, 1f, 0.05f), p.GetMaterial("metal"), new Vector3(0f, 0f, 6f));
            PrimitiveKit.MeshPart(t, MeshFactory.Umbrella, "Canopy", new Vector3(-0.2f, 1.9f, 0f), new Vector3(1.2f, 1.1f, 1.2f),
                new[] { p.GetMaterial("umbrellaWhite"), p.GetMaterial("umbrellaRed") }, new Vector3(0f, 0f, 6f));
            Lounger(t, new Vector3(0.55f, 0f, -0.2f), p);
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Ball", new Vector3(-0.7f, 0.17f, -0.6f), Vector3.one * 0.34f, p.GetMaterial("surfYellow"));
            return root;
        }

        public static GameObject BuildDeckChairs(PaletteDefinition p)
        {
            var root = new GameObject("DeckChairs");
            Lounger(root.transform, new Vector3(-0.45f, 0f, 0f), p);
            Lounger(root.transform, new Vector3(0.45f, 0f, 0f), p);
            PrimitiveKit.Part(root.transform, PrimitiveType.Cylinder, "Table", new Vector3(0f, 0.3f, -0.7f), new Vector3(0.35f, 0.02f, 0.35f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(root.transform, PrimitiveType.Cylinder, "TableLeg", new Vector3(0f, 0.15f, -0.7f), new Vector3(0.04f, 0.15f, 0.04f), p.GetMaterial("metal"));
            return root;
        }

        public static GameObject BuildLifeguardTower(PaletteDefinition p)
        {
            var root = new GameObject("LifeguardTower");
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.5f : 0.5f, z = i < 2 ? -0.5f : 0.5f;
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Leg{i}", new Vector3(x * 1.1f, 0.85f, z * 1.1f), new Vector3(0.1f, 1.7f, 0.1f), wood, new Vector3(-z * 8f, 0f, x * 8f));
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Platform", new Vector3(0f, 1.7f, 0f), new Vector3(1.3f, 0.1f, 1.3f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Hut", new Vector3(0f, 2.2f, 0.1f), new Vector3(1f, 0.9f, 0.9f), p.GetMaterial("shirtWhite"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "HutStripe", new Vector3(0f, 2.2f, -0.36f), new Vector3(1.01f, 0.18f, 0.02f), p.GetMaterial("flagRed"));
            PrimitiveKit.GableRoof(t, "Roof", new Vector3(0f, 2.65f, 0.1f), 1.3f, 1.15f, p.GetMaterial("flagRed"));
            for (int i = 0; i < 5; i++)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Rung{i}", new Vector3(0f, 0.25f + i * 0.3f, -0.95f + i * 0.08f), new Vector3(0.5f, 0.05f, 0.08f), wood);
            }

            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "FlagPole", new Vector3(0.55f, 3.1f, 0.5f), new Vector3(0.03f, 0.5f, 0.03f), p.GetMaterial("metal"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Flag", new Vector3(0.78f, 3.4f, 0.5f), new Vector3(0.45f, 0.28f, 0.02f), p.GetMaterial("flagRed"));
            return root;
        }

        public static GameObject BuildSandcastle(PaletteDefinition p)
        {
            var root = new GameObject("Sandcastle");
            Transform t = root.transform;
            Material sand = p.GetMaterial("castle");
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Keep", new Vector3(0f, 0.25f, 0f), new Vector3(0.9f, 0.5f, 0.9f), sand);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.5f : 0.5f, z = i < 2 ? -0.5f : 0.5f;
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Tower{i}", new Vector3(x, 0.35f, z), new Vector3(0.32f, 0.35f, 0.32f), sand);
                PrimitiveKit.MeshPart(t, MeshFactory.Cone, $"Spire{i}", new Vector3(x, 0.7f, z), new Vector3(0.2f, 0.32f, 0.2f), sand);
            }

            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "Moat", new Vector3(0f, 0.01f, 0f), new Vector3(1.9f, 0.01f, 1.9f), p.GetMaterial("wetSand"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Flag", new Vector3(0.1f, 0.78f, 0f), new Vector3(0.2f, 0.12f, 0.01f), p.GetMaterial("flagRed"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Bucket", new Vector3(0.95f, 0.15f, -0.6f), new Vector3(0.22f, 0.3f, 0.22f), p.GetMaterial("surfBlue"));
            return root;
        }

        public static GameObject BuildVolleyball(PaletteDefinition p)
        {
            var root = new GameObject("VolleyballNet");
            Transform t = root.transform;
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostL", new Vector3(-1.4f, 0.9f, 0f), new Vector3(0.06f, 0.9f, 0.06f), p.GetMaterial("metal"));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostR", new Vector3(1.4f, 0.9f, 0f), new Vector3(0.06f, 0.9f, 0.06f), p.GetMaterial("metal"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Net", new Vector3(0f, 1.45f, 0f), new Vector3(2.8f, 0.5f, 0.015f), p.GetMaterial("shirtWhite"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Tape", new Vector3(0f, 1.72f, 0f), new Vector3(2.8f, 0.05f, 0.03f), p.GetMaterial("flagRed"));
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Ball", new Vector3(0.6f, 0.13f, -0.7f), Vector3.one * 0.26f, p.GetMaterial("surfYellow"));
            return root;
        }

        private static void Lounger(Transform parent, Vector3 position, PaletteDefinition p)
        {
            Transform l = PrimitiveKit.Empty("Lounger", parent, position).transform;
            Material wood = p.GetMaterial("wood");
            PrimitiveKit.Part(l, PrimitiveType.Cube, "Frame", new Vector3(0f, 0.22f, 0f), new Vector3(0.55f, 0.06f, 1.4f), wood);
            PrimitiveKit.Part(l, PrimitiveType.Cube, "Cushion", new Vector3(0f, 0.28f, -0.15f), new Vector3(0.5f, 0.06f, 1f), p.GetMaterial("cushion"));
            PrimitiveKit.Part(l, PrimitiveType.Cube, "Back", new Vector3(0f, 0.45f, 0.55f), new Vector3(0.5f, 0.06f, 0.5f), p.GetMaterial("cushion"), new Vector3(-45f, 0f, 0f));
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.22f : 0.22f, z = i < 2 ? -0.6f : 0.6f;
                PrimitiveKit.Part(l, PrimitiveType.Cube, $"Leg{i}", new Vector3(x, 0.1f, z), new Vector3(0.05f, 0.2f, 0.05f), wood);
            }
        }
    }
}
