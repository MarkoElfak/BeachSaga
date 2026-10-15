using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Marker on a building prefab: where its stands and rent coins go. Swap the art freely around it.</summary>
    public sealed class BuildingView : MonoBehaviour
    {
        [SerializeField] private Transform[] standAnchors = new Transform[0];

        [Tooltip("Where rent coins appear (houses). Falls back to the building's position.")]
        [SerializeField] private Transform coinAnchor = null;

        public Vector3 CoinPosition => coinAnchor != null ? coinAnchor.position : transform.position + Vector3.up;

        public Transform GetStandAnchor(int slotIndex)
        {
            if (standAnchors == null || standAnchors.Length == 0)
            {
                return transform;
            }

            // More stands than anchors: wrap around rather than fail.
            return standAnchors[slotIndex % standAnchors.Length];
        }
    }
}
