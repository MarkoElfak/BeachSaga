using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.UI;
using TMPro;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Scene pieces for moving objects: the MoveController with its footprint indicator, and the HUD hint.</summary>
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

            var indicator = indicatorGo.AddComponent<PlacementIndicator>();
            PrimitiveKit.SetReference(indicator, "plate", plate.transform);
            PrimitiveKit.SetReference(indicator, "plateRenderer", plateRenderer);
            PrimitiveKit.SetReference(indicator, "validMaterial", valid);
            PrimitiveKit.SetReference(indicator, "invalidMaterial", database.palette.GetMaterial("placeInvalid"));

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
            RectTransform banner = UiKit.Rect("Banner", host, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -450f), new Vector2(960f, 130f));
            UiKit.AddImage(banner, new Color(0.15f, 0.45f, 0.25f, 0.92f), false);
            TextMeshProUGUI label = UiKit.ChildText(banner, "Label", "Drag to a green spot and let go", 42f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            label.margin = new Vector4(30f, 8f, 30f, 8f);

            var view = host.gameObject.AddComponent<MoveHintView>();
            PrimitiveKit.SetReference(view, "banner", banner.gameObject);
            PrimitiveKit.SetReference(view, "label", label);
        }
    }
}
