using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// The player's characters: a visitor's body a little taller, dressed from the CharacterDefinition's look, with
    /// a hat, cap or flower, a name tag over the head and a collider so a tap finds them. They face +Z.
    /// </summary>
    internal static class CharacterPrefabs
    {
        private const float Scale = 1.15f;
        private const float TagHeight = 1.9f;

        public static GameObject Build(CharacterDefinition c, PaletteDefinition p)
        {
            var root = new GameObject(c.displayName);
            Material skin = p.GetMaterial(c.skin);
            Material shirt = p.GetMaterial(c.shirt);
            Material shorts = p.GetMaterial(c.shorts);
            Material hair = p.GetMaterial(c.hair);

            Transform body = PrimitiveKit.Empty("Body", root.transform, Vector3.zero).transform;
            Transform scaled = PrimitiveKit.Empty("Figure", body, Vector3.zero).transform;
            scaled.localScale = Vector3.one * Scale;
            Transform leftLeg = VisitorPrefabs.Limb(scaled, "LeftLeg", new Vector3(-0.09f, 0.5f, 0f), shorts, skin, 0.15f, 0.2f);
            Transform rightLeg = VisitorPrefabs.Limb(scaled, "RightLeg", new Vector3(0.09f, 0.5f, 0f), shorts, skin, 0.15f, 0.2f);
            PrimitiveKit.Part(scaled, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.75f, 0f), new Vector3(0.38f, 0.25f, 0.24f), shirt);
            Transform leftArm = VisitorPrefabs.Limb(scaled, "LeftArm", new Vector3(-0.23f, 0.93f, 0f), shirt, skin, 0.11f, 0.12f);
            Transform rightArm = VisitorPrefabs.Limb(scaled, "RightArm", new Vector3(0.23f, 0.93f, 0f), shirt, skin, 0.11f, 0.12f);
            PrimitiveKit.Part(scaled, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.13f, 0f), Vector3.one * 0.25f, skin);
            PrimitiveKit.Part(scaled, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.19f, 0.025f), new Vector3(0.265f, 0.17f, 0.265f), hair);
            if (c.longHair)
            {
                PrimitiveKit.Part(scaled, PrimitiveType.Capsule, "LongHair", new Vector3(0f, 1.02f, -0.1f), new Vector3(0.26f, 0.16f, 0.12f), hair);
            }

            Headwear(scaled, c, p);
            NameTag(root.transform, c.displayName, p);
            BoxCollider tapArea = PrimitiveKit.AddBox(root, new Vector3(0f, 0.8f, 0f), new Vector3(0.6f, 1.6f, 0.6f));
            tapArea.isTrigger = true;

            var view = root.AddComponent<CharacterView>();
            PrimitiveKit.SetReference(view, "body", body);
            PrimitiveKit.SetReference(view, "leftLeg", leftLeg);
            PrimitiveKit.SetReference(view, "rightLeg", rightLeg);
            PrimitiveKit.SetReference(view, "leftArm", leftArm);
            PrimitiveKit.SetReference(view, "rightArm", rightArm);
            return root;
        }

        private static void Headwear(Transform figure, CharacterDefinition c, PaletteDefinition p)
        {
            Material material = p.GetMaterial(c.headwearColor);
            switch (c.headwear)
            {
                case CharacterHeadwear.Cap: // a sailor's cap with its peak to the front
                    PrimitiveKit.Part(figure, PrimitiveType.Cylinder, "Cap", new Vector3(0f, 1.25f, 0f), new Vector3(0.28f, 0.05f, 0.28f), material);
                    PrimitiveKit.Part(figure, PrimitiveType.Cube, "Peak", new Vector3(0f, 1.22f, 0.15f), new Vector3(0.2f, 0.02f, 0.12f), material);
                    break;
                case CharacterHeadwear.ChefHat:
                    PrimitiveKit.Part(figure, PrimitiveType.Cylinder, "HatBand", new Vector3(0f, 1.29f, 0f), new Vector3(0.25f, 0.06f, 0.25f), material);
                    PrimitiveKit.Part(figure, PrimitiveType.Sphere, "HatPuff", new Vector3(0f, 1.42f, 0f), new Vector3(0.32f, 0.2f, 0.32f), material);
                    break;
                case CharacterHeadwear.Flower:
                    PrimitiveKit.Part(figure, PrimitiveType.Sphere, "Flower", new Vector3(0.11f, 1.24f, 0.03f), Vector3.one * 0.09f, material);
                    break;
            }
        }

        // Bold white name on a dark plate, turned to the camera by NameTagView.
        private static void NameTag(Transform root, string text, PaletteDefinition p)
        {
            var go = new GameObject("NameTag", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, TagHeight, 0f);
            PrimitiveKit.Part(go.transform, PrimitiveType.Cube, "Plate", new Vector3(0f, 0f, 0.03f), new Vector3(1.1f, 0.34f, 0.02f), p.GetMaterial("shorts"));
            var label = go.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(1.05f, 0.33f);
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 2.4f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var tag = go.AddComponent<NameTagView>();
            PrimitiveKit.SetReference(tag, "label", label);
        }
    }
}
