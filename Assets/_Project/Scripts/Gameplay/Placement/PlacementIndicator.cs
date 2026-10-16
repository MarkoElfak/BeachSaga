using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Flat footprint shown under a dragged object: green where it can be dropped, red where it cannot.</summary>
    public sealed class PlacementIndicator : MonoBehaviour
    {
        private const float Thickness = 0.04f;

        [SerializeField] private Transform plate = null;
        [SerializeField] private Renderer plateRenderer = null;
        [SerializeField] private Material validMaterial = null;
        [SerializeField] private Material invalidMaterial = null;

        private bool _shownValid;

        public void Show(Rect footprint, float height, bool valid)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            plate.position = new Vector3(footprint.center.x, height + Thickness, footprint.center.y);
            plate.localScale = new Vector3(footprint.width, Thickness, footprint.height);
            if (valid != _shownValid || plateRenderer.sharedMaterial == null)
            {
                _shownValid = valid;
                plateRenderer.sharedMaterial = valid ? validMaterial : invalidMaterial;
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
