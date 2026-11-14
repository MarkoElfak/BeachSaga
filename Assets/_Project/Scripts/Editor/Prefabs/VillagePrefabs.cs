using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The village buildings (M20): a beach café and a restaurant (food places with a queue), and the service
    /// buildings, which have no queue but a reach ring: a first-aid station that hides a hospital until it grows,
    /// and the chapels (ChapelPrefabs).
    /// </summary>
    internal static class VillagePrefabs
    {
        public const string ReachRingName = "ReachRing";
        public const string BaseLookName = "BaseLook";
        public const string GrownLookName = "GrownLook";

        // A kiosk with a coffee machine and pastries under a teal roof, a table with a parasol on each side.
        public static GameObject BuildBeachCafe(PaletteDefinition p)
        {
            var root = new GameObject("BeachCafe");
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.5f, 0f), new Vector3(2f, 1f, 1f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "CounterTop", new Vector3(0f, 1.03f, 0f), new Vector3(2.1f, 0.06f, 1.1f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "BackWall", new Vector3(0f, 1.75f, 0.45f), new Vector3(2f, 1.4f, 0.15f), p.GetMaterial("plaster"));
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Post{side}", new Vector3(0.95f * side, 1.75f, -0.4f), new Vector3(0.07f, 0.75f, 0.07f), wood);
                Table(t, $"Table{side}", new Vector3(1.6f * side, 0f, -0.05f), p);
            }

            PrimitiveKit.Part(t, PrimitiveType.Cube, "Roof", new Vector3(0f, 2.5f, 0.02f), new Vector3(2.4f, 0.12f, 1.4f), p.GetMaterial("towelTeal"), new Vector3(-8f, 0f, 0f));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Machine", new Vector3(-0.5f, 1.24f, 0.15f), new Vector3(0.45f, 0.36f, 0.3f), p.GetMaterial("metal"));
            for (int i = 0; i < 2; i++)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Cup{i}", new Vector3(0.05f + 0.22f * i, 1.11f, -0.2f), new Vector3(0.12f, 0.06f, 0.12f), p.GetMaterial("shirtWhite"));
            }

            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Pastry", new Vector3(0.6f, 1.1f, -0.15f), new Vector3(0.3f, 0.1f, 0.18f), p.GetMaterial("dough"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Sign", new Vector3(0f, 2.78f, -0.58f), new Vector3(1.1f, 0.34f, 0.06f), p.GetMaterial("shirtWhite"));
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "SignCup", new Vector3(0f, 2.78f, -0.62f), new Vector3(0.22f, 0.02f, 0.22f), p.GetMaterial("dough"),
                new Vector3(90f, 0f, 0f));
            return Finish(root, p, new Vector3(3.8f, 2.9f, 1.4f), -0.55f, 3.2f, true);
        }

        // A plastered seafood restaurant under a tiled roof with a serving counter in front: two people work here.
        public static GameObject BuildRestaurant(PaletteDefinition p)
        {
            var root = new GameObject("Restaurant");
            Transform t = root.transform;
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Walls", new Vector3(0f, 0.9f, 0.4f), new Vector3(3f, 1.8f, 2f), p.GetMaterial("houseA"));
            PrimitiveKit.GableRoof(t, "Roof", new Vector3(0f, 1.8f, 0.4f), 3.3f, 2.3f, p.GetMaterial("roof"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Counter", new Vector3(0f, 0.475f, -0.85f), new Vector3(2.2f, 0.95f, 0.6f), p.GetMaterial("wood"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "CounterTop", new Vector3(0f, 0.98f, -0.85f), new Vector3(2.3f, 0.06f, 0.7f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Awning", new Vector3(0f, 1.72f, -0.85f), new Vector3(3.1f, 0.08f, 0.9f), p.GetMaterial("flagRed"), new Vector3(-12f, 0f, 0f));
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveKit.Part(t, PrimitiveType.Cube, $"Window{side}", new Vector3(1.05f * side, 1.15f, -0.61f), new Vector3(0.5f, 0.5f, 0.03f), p.GetMaterial("window"));
                PrimitiveKit.Part(t, PrimitiveType.Cylinder, $"Platter{side}", new Vector3(0.5f * side, 1.02f, -0.85f), new Vector3(0.45f, 0.02f, 0.45f), p.GetMaterial("metal"));
            }

            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Lobster", new Vector3(-0.5f, 1.07f, -0.85f), new Vector3(0.28f, 0.1f, 0.14f), p.GetMaterial("surfRed"));
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Fish", new Vector3(0.5f, 1.07f, -0.85f), new Vector3(0.3f, 0.08f, 0.12f), p.GetMaterial("surfBlue"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Sign", new Vector3(0f, 2.15f, -0.75f), new Vector3(1.5f, 0.4f, 0.06f), p.GetMaterial("shirtWhite"));
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "SignFish", new Vector3(0f, 2.15f, -0.79f), new Vector3(0.6f, 0.2f, 0.04f), p.GetMaterial("surfBlue"));
            Finish(root, p, new Vector3(3.3f, 2.9f, 2.7f), -1.2f, 3.4f, true);
            Transform upgrades = t.Find(StandUpgradePrefabs.GroupName);
            upgrades.localPosition = new Vector3(0f, 0.05f, -0.7f); // the counter stands further forward than a stand's
            upgrades.localScale = new Vector3(1.45f, 1f, 1f);
            return root;
        }

        // A white hut with a red cross and a flag; the hospital it grows into (and the ground it needs) is already there, hidden.
        public static GameObject BuildFirstAid(PaletteDefinition p)
        {
            var root = new GameObject("FirstAid");
            Transform baseLook = PrimitiveKit.Empty(BaseLookName, root.transform, Vector3.zero).transform;
            PrimitiveKit.Part(baseLook, PrimitiveType.Cube, "Walls", new Vector3(0f, 0.7f, 0.2f), new Vector3(1.8f, 1.4f, 1.4f), p.GetMaterial("plaster"));
            PrimitiveKit.GableRoof(baseLook, "Roof", new Vector3(0f, 1.4f, 0.2f), 2f, 1.6f, p.GetMaterial("flagRed"));
            RedCross(baseLook, new Vector3(0f, 0.95f, -0.51f), 0.5f, p);
            PrimitiveKit.Part(baseLook, PrimitiveType.Cylinder, "FlagPole", new Vector3(-1.15f, 1f, -0.3f), new Vector3(0.05f, 1f, 0.05f), p.GetMaterial("metal"));
            PrimitiveKit.Part(baseLook, PrimitiveType.Cube, "Flag", new Vector3(-0.88f, 1.8f, -0.3f), new Vector3(0.5f, 0.34f, 0.02f), p.GetMaterial("shirtWhite"));
            RedCross(baseLook, new Vector3(-0.88f, 1.8f, -0.32f), 0.22f, p);
            PrimitiveKit.Part(baseLook, PrimitiveType.Cube, "Lounger", new Vector3(1.25f, 0.2f, -0.1f), new Vector3(0.5f, 0.12f, 1f), p.GetMaterial("cushion"));

            Transform grown = PrimitiveKit.Empty(GrownLookName, root.transform, Vector3.zero).transform;
            PrimitiveKit.Part(grown, PrimitiveType.Cube, "Body", new Vector3(0f, 1.3f, 0.35f), new Vector3(3.4f, 2.6f, 2.1f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(grown, PrimitiveType.Cube, "RoofSlab", new Vector3(0f, 2.67f, 0.35f), new Vector3(3.6f, 0.14f, 2.3f), p.GetMaterial("stone"));
            for (int floor = 0; floor < 2; floor++)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    PrimitiveKit.Part(grown, PrimitiveType.Cube, $"Window{floor}_{i}", new Vector3(1.05f * i, 0.85f + floor * 1.2f, -0.71f),
                        new Vector3(0.6f, 0.55f, 0.03f), p.GetMaterial("window"));
                }
            }

            PrimitiveKit.Part(grown, PrimitiveType.Cube, "Door", new Vector3(0f, 0.55f, -0.71f), new Vector3(0.8f, 1.1f, 0.03f), p.GetMaterial("glass"));
            PrimitiveKit.Part(grown, PrimitiveType.Cube, "Canopy", new Vector3(0f, 1.25f, -1.05f), new Vector3(1.4f, 0.1f, 0.7f), p.GetMaterial("flagRed"));
            PrimitiveKit.Part(grown, PrimitiveType.Cube, "CrossBoard", new Vector3(0f, 3.25f, -0.45f), new Vector3(1f, 1f, 0.08f), p.GetMaterial("shirtWhite"));
            RedCross(grown, new Vector3(0f, 3.25f, -0.5f), 0.75f, p);
            grown.gameObject.SetActive(false);

            GameObject finished = Finish(root, p, new Vector3(3.6f, 3.8f, 2.6f), -1.2f, 4.2f, false);
            var view = finished.GetComponent<StandView>();
            PrimitiveKit.SetReference(view, "baseLook", baseLook.gameObject);
            PrimitiveKit.SetReference(view, "grownLook", grown.gameObject);
            return finished;
        }

        /// <summary>
        /// Coin anchor, progress bar, manager hat and a tap box for any village building. Food places get a queue
        /// and mini-game pieces; service buildings a reach ring instead.
        /// </summary>
        internal static GameObject Finish(GameObject root, PaletteDefinition p, Vector3 box, float frontZ, float barHeight, bool sells)
        {
            Transform t = root.transform;
            PrimitiveKit.AddBox(root, new Vector3(0f, box.y * 0.5f, 0f), box);
            GameObject coinAnchor = PrimitiveKit.Empty("CoinAnchor", t, new Vector3(0f, 0.7f, frontZ - 0.7f));
            ProgressBarView bar = PrimitiveKit.ProgressBar(t, new Vector3(0f, barHeight, 0f), 1.6f, p.GetMaterial("barBack"), p.GetMaterial("barFill"));
            GameObject hat = StandPrefabs.ManagerHat(t, p);
            hat.transform.localPosition = new Vector3(box.x * 0.5f - 0.3f, barHeight - 0.5f, frontZ + 0.2f);

            var view = root.AddComponent<StandView>();
            PrimitiveKit.SetReference(view, "coinAnchor", coinAnchor.transform);
            PrimitiveKit.SetReference(view, "progressBar", bar);
            PrimitiveKit.SetReference(view, "managerHat", hat);
            if (sells)
            {
                // Faces the camera (-Z): visitors line up from here toward the bottom of the screen.
                GameObject queueAnchor = PrimitiveKit.Empty("QueueAnchor", t, new Vector3(0f, 0f, frontZ - 1.2f), new Vector3(0f, 180f, 0f));
                PrimitiveKit.SetReference(view, "queueAnchor", queueAnchor.transform);
                PrimitiveKit.SetReferences(view, "upgradePieces", StandUpgradePrefabs.Build(t, p));
                return root;
            }

            GameObject ring = PrimitiveKit.MeshPart(t, MeshFactory.Ring, ReachRingName, new Vector3(0f, 0.06f, 0f), Vector3.one, p.GetMaterial("placeValid"));
            var reach = root.AddComponent<ReachRingView>();
            PrimitiveKit.SetReference(reach, "stand", view);
            PrimitiveKit.SetReference(reach, "ring", ring.transform);
            return root;
        }

        internal static void RedCross(Transform parent, Vector3 at, float size, PaletteDefinition p)
        {
            Material red = p.GetMaterial("flagRed");
            PrimitiveKit.Part(parent, PrimitiveType.Cube, "CrossH", at, new Vector3(size, size * 0.3f, 0.02f), red);
            PrimitiveKit.Part(parent, PrimitiveType.Cube, "CrossV", at, new Vector3(size * 0.3f, size, 0.02f), red);
        }

        // A round café table with two stools and a striped parasol.
        private static void Table(Transform parent, string name, Vector3 at, PaletteDefinition p)
        {
            Transform table = PrimitiveKit.Empty(name, parent, at).transform;
            PrimitiveKit.Part(table, PrimitiveType.Cylinder, "Top", new Vector3(0f, 0.72f, 0f), new Vector3(0.6f, 0.03f, 0.6f), p.GetMaterial("shirtWhite"));
            PrimitiveKit.Part(table, PrimitiveType.Cylinder, "Leg", new Vector3(0f, 0.36f, 0f), new Vector3(0.06f, 0.36f, 0.06f), p.GetMaterial("metal"));
            PrimitiveKit.Part(table, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 1.3f, 0f), new Vector3(0.04f, 0.6f, 0.04f), p.GetMaterial("metal"));
            PrimitiveKit.MeshPart(table, MeshFactory.Umbrella, "Parasol", new Vector3(0f, 1.85f, 0f), new Vector3(0.55f, 0.6f, 0.55f),
                new[] { p.GetMaterial("umbrellaRed"), p.GetMaterial("umbrellaWhite") });
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveKit.Part(table, PrimitiveType.Cylinder, $"Stool{side}", new Vector3(0f, 0.25f, 0.42f * side), new Vector3(0.28f, 0.25f, 0.28f), p.GetMaterial("wood"));
            }
        }
    }
}
