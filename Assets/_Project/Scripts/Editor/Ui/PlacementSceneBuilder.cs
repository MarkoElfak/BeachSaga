using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Scene pieces for moving objects: the MoveController with its footprint indicator (plate plus an
    /// outline of the exact footprint), the HUD hint and the rotate arrows.</summary>
    internal static class PlacementSceneBuilder
    {
        public static MoveController Build(GameDatabase database, Camera camera, PointerGestureSource gestures)
        {
            var root = new GameObject("Placement");
            var mover = root.AddComponent<MoveController>();

            GameObject indicatorGo = PrimitiveKit.Empty("FootprintIndicator", root.transform, Vector3.zero);
            Material valid = database.palette.GetMaterial("placeValid");
            GameObject plate = PrimitiveKit.Part(indicatorGo.transform, PrimitiveType.Cube, "Plate", Vector3.zero, Vector3.one, valid);
            var plateRenderer = plate.GetComponent<MeshRenderer>();
            plateRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Material validEdge = database.palette.GetMaterial("placeValidEdge");
            var edges = new Object[4];
            string[] names = { "EdgeLeft", "EdgeRight", "EdgeBack", "EdgeFront" };
            for (int i = 0; i < edges.Length; i++)
            {
                GameObject bar = PrimitiveKit.Part(indicatorGo.transform, PrimitiveType.Cube, names[i], Vector3.zero, Vector3.one, validEdge);
                var barRenderer = bar.GetComponent<MeshRenderer>();
                barRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                edges[i] = barRenderer;
            }

            var indicator = indicatorGo.AddComponent<PlacementIndicator>();
            PrimitiveKit.SetReference(indicator, "plate", plate.transform);
            PrimitiveKit.SetReference(indicator, "plateRenderer", plateRenderer);
            PrimitiveKit.SetReference(indicator, "validMaterial", valid);
            PrimitiveKit.SetReference(indicator, "invalidMaterial", database.palette.GetMaterial("placeInvalid"));
            PrimitiveKit.SetReferences(indicator, "edges", edges);
            PrimitiveKit.SetReference(indicator, "validEdgeMaterial", validEdge);
            PrimitiveKit.SetReference(indicator, "invalidEdgeMaterial", database.palette.GetMaterial("placeInvalidEdge"));

            PrimitiveKit.SetReference(mover, "worldCamera", camera);
            PrimitiveKit.SetReference(mover, "gestures", gestures);
            PrimitiveKit.SetReference(mover, "indicator", indicator);
            indicatorGo.SetActive(false);
            return mover;
        }

        public static void BuildMoveHint(Transform parent)
        {
            RectTransform host = UiKit.Stretch("MoveHint", parent);
            // Top of the screen, below the HUD buttons, so it never covers the tutorial card or the build bar.
            RectTransform banner = UiKit.Rect("Banner", host, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -510f), new Vector2(960f, 130f));
            UiKit.AddImage(banner, new Color(0.15f, 0.45f, 0.25f, 0.92f), false);
            TextMeshProUGUI label = UiKit.ChildText(banner, "Label", "Drag to a green spot and let go", 42f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            label.margin = new Vector4(30f, 8f, 30f, 8f);

            var view = host.gameObject.AddComponent<MoveHintView>();
            PrimitiveKit.SetReference(view, "banner", banner.gameObject);
            PrimitiveKit.SetReference(view, "label", label);
        }

        /// <summary>Rotate arrows (and Upgrade) that float above the selected object.</summary>
        public static void BuildSelectionArrows(Transform parent, Camera worldCamera)
        {
            RectTransform host = UiKit.Stretch("SelectionArrows", parent);
            RectTransform bar = UiKit.Rect("Bar", host, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(300f, 260f));
            Button left = BarButton(bar, "RotateLeft", "<", new Vector2(-80f, 0f), new Vector2(130f, 130f), Color.white);
            Button right = BarButton(bar, "RotateRight", ">", new Vector2(80f, 0f), new Vector2(130f, 130f), Color.white);
            Button upgrade = BarButton(bar, "UpgradeButton", "Upgrade", new Vector2(0f, 150f), new Vector2(260f, 100f), UiKit.ButtonColor);

            var view = host.gameObject.AddComponent<SelectionArrowsView>();
            PrimitiveKit.SetReference(view, "worldCamera", worldCamera);
            PrimitiveKit.SetReference(view, "area", host);
            PrimitiveKit.SetReference(view, "bar", bar);
            PrimitiveKit.SetReference(view, "leftButton", left);
            PrimitiveKit.SetReference(view, "rightButton", right);
            PrimitiveKit.SetReference(view, "upgradeButton", upgrade);
        }

        private static Button BarButton(RectTransform bar, string name, string text, Vector2 at, Vector2 size, Color color)
        {
            RectTransform rect = UiKit.Rect(name, bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), at, size);
            Button button = UiKit.AddButton(rect, color, out _);
            UiKit.ChildText(rect, "Label", text, text.Length == 1 ? 80f : 44f, TextAlignmentOptions.Center, UiKit.TextDark, FontStyles.Bold);
            return button;
        }
    }
}
