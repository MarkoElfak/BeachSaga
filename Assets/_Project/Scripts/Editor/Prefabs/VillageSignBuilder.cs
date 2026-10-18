using BeachTycoon.Data;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>The wooden welcome sign with the village name; VillageSignView stands it beside the footpath at runtime.</summary>
    internal static class VillageSignBuilder
    {
        public static VillageSignView Build(PaletteDefinition p)
        {
            var root = new GameObject("VillageSign");
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");
            // Posts stand behind the board so they never cover the name.
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostLeft", new Vector3(-0.85f, 0.75f, 0.1f), new Vector3(0.12f, 0.75f, 0.12f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostRight", new Vector3(0.85f, 0.75f, 0.1f), new Vector3(0.12f, 0.75f, 0.12f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Board", new Vector3(0f, 1.15f, 0f), new Vector3(2f, 0.75f, 0.08f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Frame", new Vector3(0f, 1.15f, 0.02f), new Vector3(2.12f, 0.87f, 0.06f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cube, "Cap", new Vector3(0f, 1.6f, 0f), new Vector3(2.3f, 0.08f, 0.24f), p.GetMaterial("roof"));

            var labelGo = new GameObject("Name", typeof(RectTransform));
            labelGo.transform.SetParent(t, false);
            var label = labelGo.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(1.6f, 0.55f);
            label.rectTransform.localPosition = new Vector3(0f, 1.15f, -0.05f);
            label.text = "Village";
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.25f, 0.15f, 0.08f);
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 3.4f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            var view = root.AddComponent<VillageSignView>();
            PrimitiveKit.SetReference(view, "label", label);
            return view;
        }
    }
}
