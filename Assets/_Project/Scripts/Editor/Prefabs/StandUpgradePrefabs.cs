using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The five decorations a stand earns from its mini-game, one per level: bunting, lanterns, planters, a gold
    /// trim and a gold star. Hidden until earned and left out of the placement footprint.
    /// </summary>
    internal static class StandUpgradePrefabs
    {
        public const string GroupName = "MiniGameUpgrades";

        public static GameObject[] Build(Transform stand, PaletteDefinition p)
        {
            Transform group = PrimitiveKit.Empty(GroupName, stand, Vector3.zero).transform;
            Material gold = p.GetMaterial("coin");
            var pieces = new GameObject[5];

            Transform bunting = Piece(group, "Bunting", pieces, 0);
            PrimitiveKit.Part(bunting, PrimitiveType.Cube, "String", new Vector3(0f, 1.98f, -0.62f), new Vector3(1.9f, 0.02f, 0.02f), p.GetMaterial("fenceRope"));
            string[] flags = { "flagRed", "towelYellow", "towelTeal", "shirtPink", "surfBlue" };
            for (int i = 0; i < flags.Length; i++)
            {
                PrimitiveKit.Part(bunting, PrimitiveType.Cube, $"Flag{i}", new Vector3(-0.76f + 0.38f * i, 1.88f, -0.62f), new Vector3(0.15f, 0.15f, 0.02f), p.GetMaterial(flags[i]), new Vector3(0f, 0f, 45f));
            }

            Transform lanterns = Piece(group, "Lanterns", pieces, 1);
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveKit.Part(lanterns, PrimitiveType.Cylinder, $"Cord{side}", new Vector3(0.88f * side, 1.62f, -0.55f), new Vector3(0.02f, 0.1f, 0.02f), p.GetMaterial("metal"));
                PrimitiveKit.Part(lanterns, PrimitiveType.Sphere, $"Lamp{side}", new Vector3(0.88f * side, 1.45f, -0.55f), new Vector3(0.2f, 0.24f, 0.2f), p.GetMaterial("umbrellaRed"));
            }

            Transform planters = Piece(group, "Planters", pieces, 2);
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveKit.Part(planters, PrimitiveType.Cylinder, $"Pot{side}", new Vector3(1.0f * side, 0.15f, -0.72f), new Vector3(0.3f, 0.15f, 0.3f), p.GetMaterial("roof"));
                PrimitiveKit.Part(planters, PrimitiveType.Sphere, $"Bush{side}", new Vector3(1.0f * side, 0.42f, -0.72f), new Vector3(0.38f, 0.34f, 0.38f), p.GetMaterial("hedge"));
            }

            Transform trim = Piece(group, "GoldTrim", pieces, 3);
            PrimitiveKit.Part(trim, PrimitiveType.Cube, "Front", new Vector3(0f, 0.62f, -0.46f), new Vector3(1.62f, 0.1f, 0.03f), gold);
            PrimitiveKit.Part(trim, PrimitiveType.Cube, "Edge", new Vector3(0f, 0.97f, -0.5f), new Vector3(1.72f, 0.04f, 0.04f), gold);

            Transform star = Piece(group, "Star", pieces, 4);
            PrimitiveKit.Part(star, PrimitiveType.Cylinder, "Pole", new Vector3(0f, 2.3f, 0.1f), new Vector3(0.05f, 0.2f, 0.05f), p.GetMaterial("metal"));
            for (int i = 0; i < 2; i++)
            {
                PrimitiveKit.Part(star, PrimitiveType.Cube, $"Point{i}", new Vector3(0f, 2.68f, 0.1f), new Vector3(0.36f, 0.36f, 0.08f), gold, new Vector3(0f, 0f, 45f * i));
            }

            return pieces;
        }

        private static Transform Piece(Transform group, string name, GameObject[] pieces, int index)
        {
            GameObject piece = PrimitiveKit.Empty(name, group, Vector3.zero);
            piece.SetActive(false);
            pieces[index] = piece;
            return piece.transform;
        }
    }
}
