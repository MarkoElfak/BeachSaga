using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Low-poly people: legs and arms on hip/shoulder pivots (swung by VisitorView), torso, head, hair or hat,
    /// and a towel shown while relaxing.
    /// </summary>
    internal static class VisitorPrefabs
    {
        public readonly struct Look
        {
            public readonly string Name, Shirt, Skin, Hair, Towel;
            public readonly bool Hat;

            public Look(string name, string shirt, string skin, string hair, string towel, bool hat)
            {
                Name = name; Shirt = shirt; Skin = skin; Hair = hair; Towel = towel; Hat = hat;
            }
        }

        public static readonly Look[] Looks =
        {
            new Look("Tourist", "shirtBlue", "skin", "hairDark", "towelRed", true),
            new Look("Foodie", "shirtOrange", "skinMid", "hairDark", "towelYellow", false),
            new Look("Sunbather", "shirtPurple", "skinDark", "hairBlond", "towelTeal", false),
            new Look("Partygoer", "shirtPink", "skin", "hairBlond", "towelYellow", false),
            new Look("Surfer", "shirtGreen", "skinMid", "hairBlond", "towelRed", false),
            new Look("Investor", "shirtWhite", "skin", "hairDark", "towelTeal", true),
        };

        public static GameObject Build(Look look, PaletteDefinition p)
        {
            var root = new GameObject(look.Name);
            Material skin = p.GetMaterial(look.Skin);
            Material shirt = p.GetMaterial(look.Shirt);
            Material shorts = p.GetMaterial("shorts");

            // Body pivots at the feet so it can be laid flat on the towel.
            Transform body = PrimitiveKit.Empty("Body", root.transform, Vector3.zero).transform;
            Transform leftLeg = Limb(body, "LeftLeg", new Vector3(-0.09f, 0.5f, 0f), shorts, skin, 0.15f, 0.2f);
            Transform rightLeg = Limb(body, "RightLeg", new Vector3(0.09f, 0.5f, 0f), shorts, skin, 0.15f, 0.2f);
            PrimitiveKit.Part(body, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.75f, 0f), new Vector3(0.38f, 0.25f, 0.24f), shirt);
            Transform leftArm = Limb(body, "LeftArm", new Vector3(-0.23f, 0.93f, 0f), shirt, skin, 0.11f, 0.12f);
            Transform rightArm = Limb(body, "RightArm", new Vector3(0.23f, 0.93f, 0f), shirt, skin, 0.11f, 0.12f);
            PrimitiveKit.Part(body, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.13f, 0f), Vector3.one * 0.25f, skin);
            PrimitiveKit.Part(body, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.19f, 0.025f), new Vector3(0.265f, 0.17f, 0.265f), p.GetMaterial(look.Hair));
            if (look.Hat)
            {
                PrimitiveKit.Part(body, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 1.24f, 0f), new Vector3(0.42f, 0.012f, 0.42f), p.GetMaterial("thatch"));
                PrimitiveKit.Part(body, PrimitiveType.Cylinder, "HatTop", new Vector3(0f, 1.29f, 0f), new Vector3(0.24f, 0.06f, 0.24f), p.GetMaterial("thatch"));
            }

            GameObject towel = PrimitiveKit.Part(root.transform, PrimitiveType.Cube, "Towel", new Vector3(0f, 0.01f, 0.55f), new Vector3(0.7f, 0.02f, 1.5f), p.GetMaterial(look.Towel));
            towel.SetActive(false);

            var view = root.AddComponent<VisitorView>();
            PrimitiveKit.SetReference(view, "body", body);
            PrimitiveKit.SetReference(view, "leftLeg", leftLeg);
            PrimitiveKit.SetReference(view, "rightLeg", rightLeg);
            PrimitiveKit.SetReference(view, "leftArm", leftArm);
            PrimitiveKit.SetReference(view, "rightArm", rightArm);
            PrimitiveKit.SetReference(view, "towel", towel);
            return root;
        }

        // A pivot with an upper part (clothes) and a lower part (skin) hanging below it.
        internal static Transform Limb(Transform body, string name, Vector3 pivot, Material upper, Material lower,
            float thickness, float upperLength)
        {
            Transform joint = PrimitiveKit.Empty(name, body, pivot).transform;
            PrimitiveKit.Part(joint, PrimitiveType.Cube, "Upper", new Vector3(0f, -upperLength * 0.5f, 0f), new Vector3(thickness, upperLength, thickness * 1.1f), upper);
            float lowerLength = pivot.y > 0.7f ? 0.24f : 0.3f;
            PrimitiveKit.Part(joint, PrimitiveType.Capsule, "Lower", new Vector3(0f, -upperLength - lowerLength * 0.5f + 0.03f, 0f),
                new Vector3(thickness * 0.75f, lowerLength * 0.5f, thickness * 0.75f), lower);
            return joint;
        }
    }
}
