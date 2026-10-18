using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The culture flagpole raised on the main building: a metal pole, a gold ball and a two-sided cloth.</summary>
    internal static class FlagpoleBuilder
    {
        private const float Height = 1.7f;
        private static readonly Vector3 ClothSize = new Vector3(0.93f, 0.62f, 1f);

        public static GameObject Build(PaletteDefinition p)
        {
            var root = new GameObject("Flagpole");
            Transform t = root.transform;
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "Pole", new Vector3(0f, Height * 0.5f, 0f), new Vector3(0.05f, Height * 0.5f, 0.05f), p.GetMaterial("metal"));
            PrimitiveKit.Part(t, PrimitiveType.Sphere, "Ball", new Vector3(0f, Height + 0.04f, 0f), new Vector3(0.11f, 0.11f, 0.11f), p.GetMaterial("coin"));

            // The cloth hangs from the top of the pole; its hoist (left edge of the image) stays at the pole.
            Transform swing = PrimitiveKit.Empty("Swing", t, new Vector3(0f, Height - 0.04f, 0f)).transform;
            Vector3 centre = new Vector3(ClothSize.x * 0.5f + 0.03f, -ClothSize.y * 0.5f, 0f);
            Material cloth = p.GetMaterial("flag");
            GameObject front = PrimitiveKit.Part(swing, PrimitiveType.Quad, "ClothFront", centre, ClothSize, cloth);
            // Turned round and mirrored, so from behind the canton is still next to the pole, as on a real flag.
            GameObject back = PrimitiveKit.Part(swing, PrimitiveType.Quad, "ClothBack", centre, new Vector3(-ClothSize.x, ClothSize.y, 1f), cloth, new Vector3(0f, 180f, 0f));

            var view = root.AddComponent<FlagView>();
            PrimitiveKit.SetReferences(view, "cloth", new Object[] { front.GetComponent<Renderer>(), back.GetComponent<Renderer>() });
            PrimitiveKit.SetReference(view, "swing", swing);
            return root;
        }
    }
}
