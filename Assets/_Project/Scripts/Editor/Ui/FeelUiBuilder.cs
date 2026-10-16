using BeachTycoon.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.EditorTools
{
    /// <summary>Screen-wide feedback layers on the HUD: the pooled confetti.</summary>
    internal static class FeelUiBuilder
    {
        private const int Pieces = 120;

        public static void BuildConfetti(Transform parent)
        {
            RectTransform layer = UiKit.Stretch("Confetti", parent);
            var pieces = new Image[Pieces];
            for (int i = 0; i < pieces.Length; i++)
            {
                Vector2 size = i % 3 == 0 ? new Vector2(26f, 46f) : new Vector2(36f, 22f);
                RectTransform rect = UiKit.Rect($"Piece{i}", layer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
                pieces[i] = UiKit.AddImage(rect, Color.white, false); // ConfettiView colours each piece on launch
                rect.gameObject.SetActive(false);
            }

            var view = layer.gameObject.AddComponent<ConfettiView>();
            PrimitiveKit.SetReference(view, "area", layer);
            PrimitiveKit.SetReferences(view, "pieces", pieces);
        }
    }
}
