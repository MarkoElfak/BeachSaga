using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Added to every spawned decoration so it can be long-pressed and moved.</summary>
    public sealed class DecorationView : MonoBehaviour, IMovable
    {
        private int _index = -1;

        public PlaceableId Placeable => PlaceableId.Decoration(_index);
        public bool CanMoveNow => _index >= 0;
        public Transform MoveRoot => transform;

        /// <summary>Gives the decoration its index and a tap/drag collider sized to its visible bounds.</summary>
        public void Bind(int index)
        {
            _index = index;
            if (GetComponent<Collider>() != null) return;

            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var box = gameObject.AddComponent<BoxCollider>();
            box.center = transform.InverseTransformPoint(bounds.center);
            Vector3 size = transform.InverseTransformVector(bounds.size);
            box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        }
    }
}
