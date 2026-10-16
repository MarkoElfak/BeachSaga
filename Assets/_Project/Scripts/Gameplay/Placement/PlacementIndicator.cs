using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Shown under a dragged object: a plate over everything it keeps free (for a stand, its queue too) and a
    /// border tracing its exact footprint. Green where it can be dropped, red where it cannot.
    /// </summary>
    public sealed class PlacementIndicator : MonoBehaviour
    {
        private const float Thickness = 0.04f;
        private const float EdgeWidth = 0.08f;
        private const float EdgeHeight = 0.06f;

        [SerializeField] private Transform plate = null;
        [SerializeField] private Renderer plateRenderer = null;
        [SerializeField] private Material validMaterial = null;
        [SerializeField] private Material invalidMaterial = null;

        [Tooltip("Four bars outlining the footprint: left, right, back, front.")]
        [SerializeField] private Renderer[] edges = new Renderer[0];
        [SerializeField] private Material validEdgeMaterial = null;
        [SerializeField] private Material invalidEdgeMaterial = null;

        private bool _shownValid;
        private bool _materialsSet;

        public void Show(Rect footprint, Rect blocking, float height, bool valid)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            plate.position = new Vector3(blocking.center.x, height + Thickness * 0.5f, blocking.center.y);
            plate.localScale = new Vector3(blocking.width, Thickness, blocking.height);
            PlaceEdges(footprint, height + Thickness + EdgeHeight * 0.5f);

            if (valid == _shownValid && _materialsSet) return;
            _shownValid = valid;
            _materialsSet = true;
            plateRenderer.sharedMaterial = valid ? validMaterial : invalidMaterial;
            Material edge = valid ? validEdgeMaterial : invalidEdgeMaterial;
            for (int i = 0; i < edges.Length; i++) edges[i].sharedMaterial = edge;
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        private void PlaceEdges(Rect r, float y)
        {
            if (edges.Length < 4) return;
            SetBar(edges[0].transform, new Vector3(r.xMin, y, r.center.y), new Vector3(EdgeWidth, EdgeHeight, r.height + EdgeWidth));
            SetBar(edges[1].transform, new Vector3(r.xMax, y, r.center.y), new Vector3(EdgeWidth, EdgeHeight, r.height + EdgeWidth));
            SetBar(edges[2].transform, new Vector3(r.center.x, y, r.yMax), new Vector3(r.width + EdgeWidth, EdgeHeight, EdgeWidth));
            SetBar(edges[3].transform, new Vector3(r.center.x, y, r.yMin), new Vector3(r.width + EdgeWidth, EdgeHeight, EdgeWidth));
        }

        private static void SetBar(Transform bar, Vector3 position, Vector3 scale)
        {
            bar.position = position;
            bar.localScale = scale;
        }
    }
}
