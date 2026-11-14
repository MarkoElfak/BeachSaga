using System.Collections.Generic;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// One prefab for every worker and job seeker: a visitor's body whose shirt, skin and hair are recoloured per
    /// worker at runtime, an apron and name tag for staff, and a gold "!" badge for job seekers. Faces +Z.
    /// </summary>
    internal static class WorkerPrefabs
    {
        private const float TagHeight = 1.7f;
        private const float BadgeHeight = 1.85f;

        public static GameObject Build(PaletteDefinition p)
        {
            var root = new GameObject("Worker");
            Material skin = p.GetMaterial("skin");
            Material shirt = p.GetMaterial("shirtBlue");
            Material hair = p.GetMaterial("hairDark");
            var shirts = new List<Renderer>();
            var skins = new List<Renderer>();

            Transform body = PrimitiveKit.Empty("Body", root.transform, Vector3.zero).transform;
            Transform leftLeg = Limb(body, "LeftLeg", new Vector3(-0.09f, 0.5f, 0f), p.GetMaterial("shorts"), skin, 0.15f, 0.2f, null, skins);
            Transform rightLeg = Limb(body, "RightLeg", new Vector3(0.09f, 0.5f, 0f), p.GetMaterial("shorts"), skin, 0.15f, 0.2f, null, skins);
            shirts.Add(Renderer(PrimitiveKit.Part(body, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.75f, 0f), new Vector3(0.38f, 0.25f, 0.24f), shirt)));
            Transform leftArm = Limb(body, "LeftArm", new Vector3(-0.23f, 0.93f, 0f), shirt, skin, 0.11f, 0.12f, shirts, skins);
            Transform rightArm = Limb(body, "RightArm", new Vector3(0.23f, 0.93f, 0f), shirt, skin, 0.11f, 0.12f, shirts, skins);
            skins.Add(Renderer(PrimitiveKit.Part(body, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.13f, 0f), Vector3.one * 0.25f, skin)));
            Renderer hairCap = Renderer(PrimitiveKit.Part(body, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.19f, 0.025f), new Vector3(0.265f, 0.17f, 0.265f), hair));
            GameObject longHair = PrimitiveKit.Part(body, PrimitiveType.Capsule, "LongHair", new Vector3(0f, 1.02f, -0.1f), new Vector3(0.26f, 0.16f, 0.12f), hair);
            GameObject apron = PrimitiveKit.Part(body, PrimitiveType.Cube, "Apron", new Vector3(0f, 0.68f, 0.115f), new Vector3(0.3f, 0.34f, 0.03f), p.GetMaterial("shirtWhite"));

            GameObject tag = NameTag(root.transform, p);
            Transform badge = Badge(root.transform, p);
            BoxCollider tapArea = PrimitiveKit.AddBox(root, new Vector3(0f, 0.85f, 0f), new Vector3(0.7f, 1.7f, 0.7f));
            tapArea.isTrigger = true;

            var view = root.AddComponent<WorkerView>();
            PrimitiveKit.SetReference(view, "body", body);
            PrimitiveKit.SetReference(view, "leftLeg", leftLeg);
            PrimitiveKit.SetReference(view, "rightLeg", rightLeg);
            PrimitiveKit.SetReference(view, "leftArm", leftArm);
            PrimitiveKit.SetReference(view, "rightArm", rightArm);
            PrimitiveKit.SetReferences(view, "shirtParts", shirts.ToArray());
            PrimitiveKit.SetReferences(view, "skinParts", skins.ToArray());
            PrimitiveKit.SetReferences(view, "hairParts", new Object[] { hairCap, longHair.GetComponent<Renderer>() });
            PrimitiveKit.SetReference(view, "longHair", longHair);
            PrimitiveKit.SetReference(view, "apron", apron);
            PrimitiveKit.SetReference(view, "nameTag", tag);
            PrimitiveKit.SetReference(view, "badge", badge);
            return root;
        }

        // VisitorPrefabs.Limb, keeping track of which parts are clothes and which are skin.
        private static Transform Limb(Transform body, string name, Vector3 pivot, Material upper, Material lower, float thickness,
            float upperLength, List<Renderer> upperParts, List<Renderer> lowerParts)
        {
            Transform joint = VisitorPrefabs.Limb(body, name, pivot, upper, lower, thickness, upperLength);
            if (upperParts != null) upperParts.Add(joint.Find("Upper").GetComponent<Renderer>());
            lowerParts.Add(joint.Find("Lower").GetComponent<Renderer>());
            return joint;
        }

        private static Renderer Renderer(GameObject part) => part.GetComponent<Renderer>();

        // A smaller version of the character's tag: white name on a dark plate.
        private static GameObject NameTag(Transform root, PaletteDefinition p)
        {
            var go = new GameObject("NameTag", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, TagHeight, 0f);
            PrimitiveKit.Part(go.transform, PrimitiveType.Cube, "Plate", new Vector3(0f, 0f, 0.03f), new Vector3(0.9f, 0.26f, 0.02f), p.GetMaterial("shorts"));
            Label(go, "Name", new Vector2(0.86f, 0.25f), 1.8f, Color.white);
            PrimitiveKit.SetReference(go.AddComponent<NameTagView>(), "label", go.GetComponent<TextMeshPro>());
            return go;
        }

        // A gold plate with a "!" over a job seeker's head, turned to the camera.
        private static Transform Badge(Transform root, PaletteDefinition p)
        {
            var go = new GameObject("Badge", typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, BadgeHeight, 0f);
            PrimitiveKit.Part(go.transform, PrimitiveType.Cube, "Plate", new Vector3(0f, 0f, 0.03f), new Vector3(0.36f, 0.36f, 0.02f), p.GetMaterial("coin"));
            Label(go, "!", new Vector2(0.34f, 0.34f), 3f, new Color(0.15f, 0.12f, 0.1f));
            PrimitiveKit.SetReference(go.AddComponent<NameTagView>(), "label", go.GetComponent<TextMeshPro>());
            return go.transform;
        }

        private static void Label(GameObject go, string text, Vector2 size, float maxSize, Color color)
        {
            var label = go.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = size;
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 0.8f;
            label.fontSizeMax = maxSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
