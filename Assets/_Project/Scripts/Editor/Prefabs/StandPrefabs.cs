using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Product stands. Each has a counter, a coin anchor, a progress bar and a queue anchor.</summary>
    internal static class StandPrefabs
    {
        /// <summary>The hat shown while a manager runs the stand; left out of the placement footprint.</summary>
        public const string ManagerHatName = "ManagerHat";

        public static GameObject BuildDonutStand(PaletteDefinition p)
        {
            GameObject root = Counter("DonutStand", p, p.GetMaterial("awning"));
            Transform t = root.transform;
            AddDonut(t, "CounterDonut", new Vector3(-0.3f, 0.98f, -0.15f), Vector3.zero, 0.45f, p);
            AddDonut(t, "CounterDonut2", new Vector3(0.3f, 0.98f, -0.1f), Vector3.zero, 0.4f, p);
            AddDonut(t, "SignDonut", new Vector3(0f, 2.2f, 0.2f), new Vector3(-35f, 0f, 0f), 0.85f, p);
            return Finish(root, p);
        }

        public static GameObject BuildTowelStand(PaletteDefinition p)
        {
            GameObject root = Counter("TowelStand", p, p.GetMaterial("towelTeal"));
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");

            // A rack of hanging towels behind the counter, plus a folded pile on top.
            PrimitiveKit.Part(t, PrimitiveType.Cube, "RackBar", new Vector3(0f, 2.15f, 0.45f), new Vector3(1.8f, 0.06f, 0.06f), wood);
            string[] colors = { "towelRed", "towelYellow", "towelTeal" };
            for (int i = 0; i < colors.Length; i++)
            {
                float x = -0.55f + 0.55f * i;
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Towel{i}", new Vector3(x, 1.75f, 0.45f), new Vector3(0.42f, 0.8f, 0.04f), p.GetMaterial(colors[i]));
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Folded{i}", new Vector3(x, 1.0f + 0.07f * i, -0.15f), new Vector3(0.45f, 0.07f, 0.35f), p.GetMaterial(colors[(i + 1) % colors.Length]));
            }

            return Finish(root, p);
        }

        public static GameObject BuildDrinksBar(PaletteDefinition p)
        {
            var root = new GameObject("DrinksBar");
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 1f, 0.9f), p.GetMaterial("thatch"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "CounterTop", new Vector3(0f, 1.03f, -0.05f), new Vector3(1.85f, 0.08f, 1.05f), wood);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.8f : 0.8f, z = i < 2 ? -0.4f : 0.4f;
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Post{i}", new Vector3(x, 1.5f, z), new Vector3(0.08f, 0.5f, 0.08f), wood);
            }

            PrimitiveKit.MeshPart(t, MeshFactory.Pyramid, "Roof", new Vector3(0f, 1.98f, 0f), new Vector3(1.55f, 0.75f, 1.05f), p.GetMaterial("thatch"), new Vector3(0f, 45f, 0f));
            string[] bottles = { "bottle", "glass", "bottle", "icing" };
            for (int i = 0; i < bottles.Length; i++)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Bottle{i}", new Vector3(-0.55f + i * 0.36f, 1.22f, 0.15f), new Vector3(0.09f, 0.13f, 0.09f), p.GetMaterial(bottles[i]));
            }

            PrimitiveKit.MeshPart(t, MeshFactory.Cone, "Cocktail", new Vector3(0.45f, 1.08f, -0.3f), new Vector3(0.12f, 0.2f, 0.12f), p.GetMaterial("towelYellow"), new Vector3(180f, 0f, 0f));
            return Finish(root, p);
        }

        public static GameObject BuildSurfSchool(PaletteDefinition p)
        {
            GameObject root = Counter("SurfSchool", p, p.GetMaterial("surfBlue"));
            Transform t = root.transform;
            string[] boards = { "surfYellow", "surfRed", "surfBlue" };
            for (int i = 0; i < boards.Length; i++)
            {
                PrimitiveKit.MeshPart(t, MeshFactory.Surfboard, $"Board{i}", new Vector3(-0.55f + i * 0.55f, 1.15f, 0.62f),
                    new Vector3(1f, 1f, 0.6f), p.GetMaterial(boards[i]), new Vector3(-78f, 0f, 0f));
            }

            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "FlagPole", new Vector3(0.95f, 1.6f, -0.3f), new Vector3(0.04f, 0.8f, 0.04f), p.GetMaterial("metal"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Flag", new Vector3(1.18f, 2.2f, -0.3f), new Vector3(0.45f, 0.3f, 0.02f), p.GetMaterial("flagRed"));
            return Finish(root, p);
        }

        public static GameObject BuildRealEstate(PaletteDefinition p)
        {
            var root = new GameObject("RealEstateOffice");
            Transform t = root.transform;
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Booth", new Vector3(0f, 0.9f, 0.15f), new Vector3(1.7f, 1.8f, 1f), p.GetMaterial("plaster"));
            PrimitiveKit.GableRoof(t, "Roof", new Vector3(0f, 1.8f, 0.15f), 2f, 1.2f, p.GetMaterial("roofBlue"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Desk", new Vector3(0f, 0.95f, -0.45f), new Vector3(1.5f, 0.08f, 0.35f), p.GetMaterial("wood"));
            BuildingPrefabs.Window(t, "Window", new Vector3(0f, 1.35f, -0.36f), p, 1.1f, 0.5f);
            // A model house on the desk and a "for sale" sign.
            PrimitiveKit.Part(t, PrimitiveType.Cube, "ModelHouse", new Vector3(-0.4f, 1.08f, -0.45f), new Vector3(0.24f, 0.18f, 0.2f), p.GetMaterial("houseC"));
            PrimitiveKit.GableRoof(t, "ModelRoof", new Vector3(-0.4f, 1.17f, -0.45f), 0.3f, 0.24f, p.GetMaterial("roof"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "SignPost", new Vector3(1.05f, 0.6f, -0.4f), new Vector3(0.06f, 1.2f, 0.06f), p.GetMaterial("wood"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "SignBoard", new Vector3(1.05f, 1.15f, -0.44f), new Vector3(0.6f, 0.4f, 0.04f), p.GetMaterial("shirtWhite"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "SignBand", new Vector3(1.05f, 1.15f, -0.47f), new Vector3(0.5f, 0.1f, 0.01f), p.GetMaterial("flagRed"));
            return Finish(root, p);
        }

        // Counter, top and awning posts shared by the simple stands.
        private static GameObject Counter(string name, PaletteDefinition p, Material awning)
        {
            var root = new GameObject(name);
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.9f, 0.9f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "CounterTop", new Vector3(0f, 0.93f, 0f), new Vector3(1.7f, 0.06f, 1f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostL", new Vector3(-0.75f, 1.3f, 0.35f), new Vector3(0.07f, 0.4f, 0.07f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostR", new Vector3(0.75f, 1.3f, 0.35f), new Vector3(0.07f, 0.4f, 0.07f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Awning", new Vector3(0f, 1.75f, 0.1f), new Vector3(1.9f, 0.1f, 1.2f), awning, new Vector3(-12f, 0f, 0f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "AwningTrim", new Vector3(0f, 1.62f, -0.5f), new Vector3(1.9f, 0.14f, 0.04f), p.GetMaterial("plaster"), new Vector3(-12f, 0f, 0f));
            return root;
        }

        private static GameObject Finish(GameObject root, PaletteDefinition p)
        {
            Transform t = root.transform;
            PrimitiveKit.AddBox(root, new Vector3(0f, 1.1f, 0f), new Vector3(1.9f, 2.2f, 1.4f));
            GameObject coinAnchor = PrimitiveKit.Empty("CoinAnchor", t, new Vector3(0f, 0.7f, -1.2f));
            // Faces the camera (-Z): visitors line up from here toward the bottom of the screen.
            GameObject queueAnchor = PrimitiveKit.Empty("QueueAnchor", t, new Vector3(0f, 0f, -1.7f), new Vector3(0f, 180f, 0f));
            ProgressBarView bar = PrimitiveKit.ProgressBar(t, new Vector3(0f, 2.9f, 0f), 1.4f, p.GetMaterial("barBack"), p.GetMaterial("barFill"));
            GameObject hat = ManagerHat(t, p);
            GameObject[] upgrades = StandUpgradePrefabs.Build(t, p);

            var view = root.AddComponent<StandView>();
            PrimitiveKit.SetReference(view, "coinAnchor", coinAnchor.transform);
            PrimitiveKit.SetReference(view, "progressBar", bar);
            PrimitiveKit.SetReference(view, "queueAnchor", queueAnchor.transform);
            PrimitiveKit.SetReference(view, "managerHat", hat);
            PrimitiveKit.SetReferences(view, "upgradePieces", upgrades);
            return root;
        }

        // A navy cap with a gold band above the stand's corner: the stand has a manager. Hidden until hired.
        private static GameObject ManagerHat(Transform parent, PaletteDefinition p)
        {
            GameObject hat = PrimitiveKit.Empty(ManagerHatName, parent, new Vector3(0.95f, 2.45f, -0.35f), new Vector3(-15f, 0f, 0f));
            Material navy = p.GetMaterial("shorts");
            PrimitiveKit.Part(hat.transform, PrimitiveType.Cylinder, "Brim", Vector3.zero, new Vector3(0.75f, 0.03f, 0.75f), navy);
            PrimitiveKit.Part(hat.transform, PrimitiveType.Cylinder, "Crown", new Vector3(0f, 0.2f, 0f), new Vector3(0.5f, 0.18f, 0.5f), navy);
            PrimitiveKit.Part(hat.transform, PrimitiveType.Cylinder, "Band", new Vector3(0f, 0.08f, 0f), new Vector3(0.52f, 0.05f, 0.52f), p.GetMaterial("coin"));
            hat.SetActive(false);
            return hat;
        }

        // Dough disc, icing disc and a dark centre dot standing in for the hole.
        private static void AddDonut(Transform parent, string name, Vector3 position, Vector3 euler, float size, PaletteDefinition p)
        {
            Transform donut = PrimitiveKit.Empty(name, parent, position, euler).transform;
            PrimitiveKit.Part(donut, PrimitiveType.Cylinder, "Dough", Vector3.zero, new Vector3(size, 0.06f, size), p.GetMaterial("dough"));
            PrimitiveKit.Part(donut, PrimitiveType.Cylinder, "Icing", new Vector3(0f, 0.05f, 0f), new Vector3(size * 0.82f, 0.02f, size * 0.82f), p.GetMaterial("icing"));
            PrimitiveKit.Part(donut, PrimitiveType.Cylinder, "Hole", new Vector3(0f, 0.08f, 0f), new Vector3(size * 0.28f, 0.02f, size * 0.28f), p.GetMaterial("wood"));
        }
    }
}
