using BeachTycoon.Data;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>A tall wooden sign for locked land: the board stands above the fog and leans back towards the camera.</summary>
    internal static class LandSignBuilder
    {
        private const float BoardHeight = 3.2f;
        private const float Lean = 22f;

        public static LandSignView Build(PaletteDefinition p)
        {
            var root = new GameObject("LandSign");
            Transform t = root.transform;
            Material wood = p.GetMaterial("wood");
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostLeft", new Vector3(-1f, 1.7f, 0.15f), new Vector3(0.14f, 1.7f, 0.14f), wood);
            PrimitiveKit.Part(t, PrimitiveType.Cylinder, "PostRight", new Vector3(1f, 1.7f, 0.15f), new Vector3(0.14f, 1.7f, 0.14f), wood);

            Transform board = new GameObject("Board").transform;
            board.SetParent(t, false);
            board.SetLocalPositionAndRotation(new Vector3(0f, BoardHeight, 0f), Quaternion.Euler(Lean, 0f, 0f));
            PrimitiveKit.Part(board, PrimitiveType.Cube, "Face", Vector3.zero, new Vector3(2.6f, 1.2f, 0.08f), p.GetMaterial("plaster"));
            PrimitiveKit.Part(board, PrimitiveType.Cube, "Frame", new Vector3(0f, 0f, 0.03f), new Vector3(2.74f, 1.34f, 0.06f), wood);
            PrimitiveKit.Part(board, PrimitiveType.Cube, "Cap", new Vector3(0f, 0.72f, 0f), new Vector3(2.9f, 0.1f, 0.26f), p.GetMaterial("roof"));

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(board, false);
            var label = labelGo.AddComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(2.3f, 1f);
            label.rectTransform.localPosition = new Vector3(0f, 0f, -0.05f);
            label.text = "Land\nOpens at level 2";
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.25f, 0.15f, 0.08f);
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = 3f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;

            var view = root.AddComponent<LandSignView>();
            PrimitiveKit.SetReference(view, "label", label);
            return view;
        }
    }
}
