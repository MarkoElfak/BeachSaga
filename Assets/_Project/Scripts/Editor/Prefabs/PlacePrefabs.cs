using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Each country's own place, built like the other stands (counter, coin and queue anchors, progress bar):
    /// a Greek taverna, a Spanish tapas bar and an Italian gelateria.
    /// </summary>
    internal static class PlacePrefabs
    {
        // Whitewashed counter under a blue roof, a grill with souvlaki skewers and a moussaka dish.
        public static GameObject BuildTaverna(PaletteDefinition p)
        {
            GameObject root = StandPrefabs.Counter("Taverna", p, p.GetMaterial("roofBlue"));
            Transform t = root.transform;
            t.Find("Counter").GetComponent<MeshRenderer>().sharedMaterial = p.GetMaterial("plaster");
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Grill", new Vector3(-0.35f, 1.0f, -0.05f), new Vector3(0.7f, 0.08f, 0.45f), p.GetMaterial("metal"));
            for (int i = 0; i < 3; i++)
            {
                float z = -0.2f + i * 0.15f;
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Skewer{i}", new Vector3(-0.35f, 1.08f, z), new Vector3(0.06f, 0.3f, 0.06f),
                    p.GetMaterial("dough"), new Vector3(0f, 0f, 90f));
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Moussaka", new Vector3(0.4f, 1.02f, -0.1f), new Vector3(0.45f, 0.1f, 0.35f), p.GetMaterial("towelYellow"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "SignBoard", new Vector3(0f, 2.05f, 0.2f), new Vector3(1.1f, 0.3f, 0.05f), p.GetMaterial("plaster"),
                new Vector3(-12f, 0f, 0f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "SignStripe", new Vector3(0f, 2.05f, 0.17f), new Vector3(0.9f, 0.08f, 0.02f), p.GetMaterial("roofBlue"),
                new Vector3(-12f, 0f, 0f));
            return StandPrefabs.Finish(root, p);
        }

        // Red and yellow awning, a big paella pan with prawns and a row of tapas plates.
        public static GameObject BuildTapasBar(PaletteDefinition p)
        {
            GameObject root = StandPrefabs.Counter("TapasBar", p, p.GetMaterial("flagRed"));
            Transform t = root.transform;
            PrimitiveKit.Part(t, PrimitiveType.Cube, "AwningStripe", new Vector3(0f, 1.81f, 0.1f), new Vector3(1.92f, 0.04f, 0.4f), p.GetMaterial("towelYellow"),
                new Vector3(-12f, 0f, 0f));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PaellaPan", new Vector3(-0.3f, 0.99f, -0.05f), new Vector3(0.75f, 0.03f, 0.75f), p.GetMaterial("metal"));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "Rice", new Vector3(-0.3f, 1.02f, -0.05f), new Vector3(0.65f, 0.02f, 0.65f), p.GetMaterial("towelYellow"));
            for (int i = 0; i < 4; i++)
            {
                float angle = i * Mathf.PI * 0.5f + 0.4f;
                Vector3 at = new Vector3(-0.3f + Mathf.Cos(angle) * 0.18f, 1.05f, -0.05f + Mathf.Sin(angle) * 0.18f);
                PrimitiveKit.Part(t, PrimitiveType.Sphere, $"Prawn{i}", at, new Vector3(0.1f, 0.05f, 0.1f), p.GetMaterial("surfRed"));
            }

            for (int i = 0; i < 2; i++)
            {
                Vector3 at = new Vector3(0.35f + i * 0.28f, 0.98f, -0.2f + i * 0.2f);
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Plate{i}", at, new Vector3(0.24f, 0.02f, 0.24f), p.GetMaterial("plaster"));
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Tapa{i}", at + new Vector3(0f, 0.04f, 0f), new Vector3(0.12f, 0.06f, 0.08f), p.GetMaterial(i == 0 ? "dough" : "bottle"));
            }

            return StandPrefabs.Finish(root, p);
        }

        // Green, white and red awning, a gelato case with three tubs and a cone sign, plus a pizza slice.
        public static GameObject BuildGelateria(PaletteDefinition p)
        {
            GameObject root = StandPrefabs.Counter("Gelateria", p, p.GetMaterial("shirtWhite"));
            Transform t = root.transform;
            string[] stripes = { "shirtGreen", "flagRed" };
            for (int i = 0; i < stripes.Length; i++)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"AwningStripe{i}", new Vector3(i == 0 ? -0.64f : 0.64f, 1.81f, 0.1f), new Vector3(0.62f, 0.04f, 1.22f),
                    p.GetMaterial(stripes[i]), new Vector3(-12f, 0f, 0f));
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Case", new Vector3(-0.2f, 1.06f, 0f), new Vector3(1f, 0.18f, 0.6f), p.GetMaterial("glass"));
            string[] flavours = { "icing", "towelYellow", "bottle" };
            for (int i = 0; i < flavours.Length; i++)
            {
                PrimitiveKit.Part(t, PrimitiveType.Sphere, $"Scoop{i}", new Vector3(-0.5f + i * 0.3f, 1.18f, 0f), new Vector3(0.24f, 0.14f, 0.24f), p.GetMaterial(flavours[i]));
            }

            PrimitiveKit.MeshPart(t, MeshFactory.Cone, "SignCone", new Vector3(0f, 2.15f, 0.25f), new Vector3(0.22f, 0.45f, 0.22f), p.GetMaterial("dough"),
                new Vector3(180f, 0f, 0f));
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "SignScoop", new Vector3(0f, 2.22f, 0.25f), new Vector3(0.36f, 0.3f, 0.36f), p.GetMaterial("icing"));
            PrimitiveKit.MeshPart(t, MeshFactory.Pyramid, "PizzaSlice", new Vector3(0.55f, 1.0f, -0.15f), new Vector3(0.25f, 0.03f, 0.35f), p.GetMaterial("surfRed"));
            return StandPrefabs.Finish(root, p);
        }
    }
}
