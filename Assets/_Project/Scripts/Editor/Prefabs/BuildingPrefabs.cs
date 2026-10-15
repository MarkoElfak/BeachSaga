using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Main building placeholders for tiers 1-3 (Tent, Cabin, Bungalow). Every tier shares the same stand
    /// anchors in a row in front, so stands keep their places across upgrades.
    /// </summary>
    internal static class BuildingPrefabs
    {
        // Unlock order: donuts right, towels left, drinks far right, surf far left, houses outermost right.
        private static readonly Vector3[] StandAnchors =
        {
            new Vector3(3.4f, 0f, -0.6f), new Vector3(-3.4f, 0f, -0.6f), new Vector3(5.6f, 0f, -0.6f),
            new Vector3(-5.6f, 0f, -0.6f), new Vector3(7.8f, 0f, -0.6f),
        };

        public static GameObject BuildTent(PaletteDefinition p)
        {
            var root = new GameObject("Tent");
            Transform t = root.transform;
            Material canvas = p.GetMaterial("tentCanvas");
            Material stripe = p.GetMaterial("tentStripe");
            Material wood = p.GetMaterial("wood");

            // A cube rolled 45 degrees and half sunk into the sand reads as a ridge tent.
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Canvas", Vector3.zero, new Vector3(2.2f, 2.2f, 3f), canvas, new Vector3(0f, 0f, 45f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "StripeFront", new Vector3(0f, 0f, -1.05f), new Vector3(2.25f, 2.25f, 0.35f), stripe, new Vector3(0f, 0f, 45f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "StripeBack", new Vector3(0f, 0f, 1.05f), new Vector3(2.25f, 2.25f, 0.35f), stripe, new Vector3(0f, 0f, 45f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Door", new Vector3(0f, 0.45f, -1.52f), new Vector3(0.7f, 0.9f, 0.05f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "FlagPole", new Vector3(0f, 1.9f, 0f), new Vector3(0.05f, 0.45f, 0.05f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Flag", new Vector3(0.25f, 2.2f, 0f), new Vector3(0.45f, 0.28f, 0.03f), stripe);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "Peg0", new Vector3(-1.5f, 0.1f, -1.4f), new Vector3(0.05f, 0.1f, 0.05f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "Peg1", new Vector3(1.5f, 0.1f, -1.4f), new Vector3(0.05f, 0.1f, 0.05f), wood);
            PrimitiveKit.AddBox(root, new Vector3(0f, 0.8f, 0f), new Vector3(3.2f, 1.6f, 3.1f));
            AddView(root);
            return root;
        }

        public static GameObject BuildCabin(PaletteDefinition p)
        {
            var root = new GameObject("Cabin");
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");
            Material white = p.GetMaterial("plaster");

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Foundation", new Vector3(0f, 0.08f, 0f), new Vector3(3.6f, 0.16f, 3f), p.GetMaterial("stone"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Walls", new Vector3(0f, 0.95f, 0f), new Vector3(3.4f, 1.6f, 2.8f), wood);
            PrimitiveKit.GableRoof(t, "Roof", new Vector3(0f, 1.75f, 0f), 3.9f, 3.3f, p.GetMaterial("roof"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Door", new Vector3(0f, 0.65f, -1.42f), new Vector3(0.7f, 1.1f, 0.05f), white);
            Window(t, "WindowL", new Vector3(-1.05f, 1.05f, -1.42f), p);
            Window(t, "WindowR", new Vector3(1.05f, 1.05f, -1.42f), p);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Porch", new Vector3(0f, 0.1f, -1.85f), new Vector3(2.4f, 0.12f, 0.9f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Chimney", new Vector3(1f, 2.6f, 0.6f), new Vector3(0.35f, 1f, 0.35f), p.GetMaterial("stone"));
            PrimitiveKit.AddBox(root, new Vector3(0f, 1.5f, 0f), new Vector3(3.6f, 3f, 3f));
            AddView(root);
            return root;
        }

        public static GameObject BuildBungalow(PaletteDefinition p)
        {
            var root = new GameObject("Bungalow");
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");

            for (int i = 0; i < 6; i++)
            {
                float x = i % 3 == 0 ? -1.9f : i % 3 == 1 ? 0f : 1.9f;
                float z = i < 3 ? -1.3f : 1.9f;
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Stilt{i}", new Vector3(x, 0.3f, z), new Vector3(0.18f, 0.3f, 0.18f), wood);
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Deck", new Vector3(0f, 0.62f, 0.3f), new Vector3(4.3f, 0.14f, 3.6f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Walls", new Vector3(0f, 1.45f, 0.6f), new Vector3(3.2f, 1.5f, 2.6f), p.GetMaterial("plaster"));
            PrimitiveKit.MeshPart(t, MeshFactory.Pyramid, "Roof", new Vector3(0f, 2.15f, 0.6f), new Vector3(3.1f, 1.6f, 2.6f), p.GetMaterial("thatch"), new Vector3(0f, 45f, 0f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Door", new Vector3(0f, 1.2f, -0.72f), new Vector3(0.7f, 1.05f, 0.05f), wood);
            Window(t, "WindowL", new Vector3(-1.05f, 1.55f, -0.72f), p);
            Window(t, "WindowR", new Vector3(1.05f, 1.55f, -0.72f), p);
            for (int i = 0; i < 7; i++)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Rail{i}", new Vector3(-1.95f + i * 0.65f, 0.9f, -1.4f), new Vector3(0.06f, 0.45f, 0.06f), wood);
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Handrail", new Vector3(0f, 1.12f, -1.4f), new Vector3(4.1f, 0.06f, 0.08f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Step0", new Vector3(0f, 0.18f, -1.62f), new Vector3(1f, 0.12f, 0.3f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Step1", new Vector3(0f, 0.4f, -1.38f), new Vector3(1f, 0.12f, 0.3f), wood);
            PrimitiveKit.AddBox(root, new Vector3(0f, 1.6f, 0.3f), new Vector3(4.3f, 3.2f, 3.6f));
            AddView(root);
            return root;
        }

        public static void Window(Transform parent, string name, Vector3 position, PaletteDefinition p, float width = 0.6f, float height = 0.5f)
        {
            PrimitiveKit.Part(parent, PrimitiveType.Cube, name + "Frame", position, new Vector3(width + 0.1f, height + 0.1f, 0.04f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(parent, PrimitiveType.Cube, name, position + new Vector3(0f, 0f, -0.015f), new Vector3(width, height, 0.04f), p.GetMaterial("glass"));
        }

        public static void AddView(GameObject root, Vector3? coinAnchor = null)
        {
            var anchors = new Transform[StandAnchors.Length];
            for (int i = 0; i < anchors.Length; i++)
            {
                anchors[i] = PrimitiveKit.Empty($"StandAnchor{i}", root.transform, StandAnchors[i]).transform;
            }

            var view = root.AddComponent<BuildingView>();
            PrimitiveKit.SetReferences(view, "standAnchors", anchors);
            if (coinAnchor.HasValue)
            {
                PrimitiveKit.SetReference(view, "coinAnchor", PrimitiveKit.Empty("CoinAnchor", root.transform, coinAnchor.Value).transform);
            }
        }
    }
}
