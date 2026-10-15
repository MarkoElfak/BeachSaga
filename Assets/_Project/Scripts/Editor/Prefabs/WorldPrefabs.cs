using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Lot, palm and coin pickup placeholders.</summary>
    internal static class WorldPrefabs
    {
        public static GameObject BuildLot(PaletteDefinition p)
        {
            var root = new GameObject("Lot");
            Transform t = root.transform;
            Material white = p.GetMaterial("foam");
            Material wood = p.GetMaterial("wood");

            GameObject marker = PrimitiveKit.Empty("EmptyMarker", t, Vector3.zero);
            PrimitiveKit.Part(marker.transform, PrimitiveType.Cube, "Pad", new Vector3(0f, 0.01f, 0f), new Vector3(3.6f, 0.04f, 3.6f), p.GetMaterial("lotMarker"));
            PrimitiveKit.Part(marker.transform, PrimitiveType.Cube, "PlusH", new Vector3(0f, 0.04f, 0f), new Vector3(1.4f, 0.04f, 0.35f), white);
            PrimitiveKit.Part(marker.transform, PrimitiveType.Cube, "PlusV", new Vector3(0f, 0.04f, 0f), new Vector3(0.35f, 0.04f, 1.4f), white);

            GameObject construction = PrimitiveKit.Empty("Construction", t, Vector3.zero);
            PrimitiveKit.Part(construction.transform, PrimitiveType.Cube, "Crate", new Vector3(0f, 0.6f, 0f), new Vector3(2.2f, 1.2f, 2.2f), wood);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -1.25f : 1.25f;
                float z = i < 2 ? -1.25f : 1.25f;
                PrimitiveKit.Part(construction.transform, PrimitiveType.Cylinder, $"Post{i}", new Vector3(x, 1f, z), new Vector3(0.08f, 1f, 0.08f), wood);
            }

            ProgressBarView bar = PrimitiveKit.ProgressBar(t, new Vector3(0f, 3.6f, 0f), 2.4f, p.GetMaterial("barBack"), p.GetMaterial("barFill"));
            GameObject buildingRoot = PrimitiveKit.Empty("BuildingRoot", t, Vector3.zero);
            GameObject standsRoot = PrimitiveKit.Empty("StandsRoot", t, Vector3.zero);
            PrimitiveKit.AddBox(root, new Vector3(0f, 0.1f, 0f), new Vector3(4f, 0.2f, 4f));

            var view = root.AddComponent<LotView>();
            PrimitiveKit.SetReference(view, "emptyMarker", marker);
            PrimitiveKit.SetReference(view, "constructionVisual", construction.transform);
            PrimitiveKit.SetReference(view, "progressBar", bar);
            PrimitiveKit.SetReference(view, "buildingRoot", buildingRoot.transform);
            PrimitiveKit.SetReference(view, "standsRoot", standsRoot.transform);
            return root;
        }

        public static GameObject BuildPalm(PaletteDefinition p)
        {
            var root = new GameObject("Palm");
            Transform t = root.transform;
            Material trunk = p.GetMaterial("palmTrunk");
            Material leaf = p.GetMaterial("palmLeaf");
            RenderingAssets.SetDoubleSided(leaf);

            PrimitiveKit.MeshPart(t, MeshFactory.PalmTrunk, "Trunk", Vector3.zero, Vector3.one, trunk);
            var crown = new Vector3(0.54f, 2.95f, 0f); // top of the curved trunk mesh
            const int fronds = 9;
            for (int i = 0; i < fronds; i++)
            {
                float yaw = i * (360f / fronds) + (i % 2) * 12f;
                float pitch = i % 3 == 0 ? -12f : i % 3 == 1 ? 4f : 14f;
                float scale = 0.95f + (i % 4) * 0.08f;
                PrimitiveKit.MeshPart(t, MeshFactory.PalmFrond, $"Frond{i}", crown, Vector3.one * scale, leaf, new Vector3(pitch, yaw, 0f));
            }

            Material coconut = p.GetMaterial("hairDark");
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Coconut0", crown + new Vector3(0.12f, -0.18f, 0.1f), Vector3.one * 0.2f, coconut);
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Coconut1", crown + new Vector3(-0.1f, -0.2f, -0.08f), Vector3.one * 0.2f, coconut);
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Coconut2", crown + new Vector3(0.02f, -0.22f, -0.14f), Vector3.one * 0.18f, coconut);
            return root;
        }

        public static GameObject BuildCoin(PaletteDefinition p)
        {
            var root = new GameObject("CoinPickup");
            GameObject visual = PrimitiveKit.Empty("Visual", root.transform, Vector3.zero);
            PrimitiveKit.Part(visual.transform, PrimitiveType.Cylinder, "Disc", Vector3.zero, new Vector3(0.6f, 0.05f, 0.6f), p.GetMaterial("coin"), new Vector3(90f, 0f, 0f));
            PrimitiveKit.Part(visual.transform, PrimitiveType.Cylinder, "Face", new Vector3(0f, 0f, -0.03f), new Vector3(0.42f, 0.02f, 0.42f), p.GetMaterial("dough"), new Vector3(90f, 0f, 0f));

            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 0.6f; // generous for thumbs

            var view = root.AddComponent<CoinPickupView>();
            PrimitiveKit.SetReference(view, "visual", visual.transform);
            return root;
        }
    }
}
