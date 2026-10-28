using BeachTycoon.Core;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// A service building's reach drawn on the ground while it is selected: the stands inside it get its help. The
    /// band follows the ground's height all the way round, so a slope never hides part of it.
    /// </summary>
    public sealed class ReachRingView : MonoBehaviour
    {
        private const int Segments = 64;
        private const float Width = 0.5f;
        private const float Lift = 0.08f;

        [SerializeField] private StandView stand = null;
        [SerializeField] private Transform ring = null;

        private readonly Vector3[] _vertices = new Vector3[Segments * 2];
        private Mesh _mesh;

        public bool IsShown => ring != null && ring.gameObject.activeSelf;

        private void Start()
        {
            _mesh = new Mesh { name = "ReachRing" };
            _mesh.vertices = _vertices;
            var triangles = new int[Segments * 6];
            for (int i = 0; i < Segments; i++)
            {
                int next = (i + 1) % Segments, t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = i * 2 + 1;
                triangles[t + 2] = next * 2;
                triangles[t + 3] = next * 2;
                triangles[t + 4] = i * 2 + 1;
                triangles[t + 5] = next * 2 + 1;
            }

            _mesh.triangles = triangles;
            ring.GetComponent<MeshFilter>().sharedMesh = _mesh;
            ring.gameObject.SetActive(false);
            EventBus.Subscribe<SelectionChanged>(OnSelectionChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<SelectionChanged>(OnSelectionChanged);
            if (_mesh != null) Destroy(_mesh);
        }

        private void OnSelectionChanged(SelectionChanged evt)
        {
            ring.gameObject.SetActive(false);
            if (!evt.HasSelection || stand.Stand == null || !ServiceLocator.TryGet(out SelectionService selection)
                || !selection.Selected.Equals(stand.Placeable) || !ServiceLocator.TryGet(out ServiceBuildings reach)) return;

            // Measured from the building's middle, like the reach itself.
            PlacementService placement = ServiceLocator.Get<PlacementService>();
            Vector2 centre = placement.GetFootprint(stand.Placeable, placement.GetStandPosition(stand.Stand.Index)).center;
            Build(centre, reach.Reach(stand.Stand));
            ring.gameObject.SetActive(true);
        }

        // Outer and inner edge points on the ground, in the ring's own space (the building may be turned).
        private void Build(Vector2 centre, float radius)
        {
            ServiceLocator.TryGet(out TerrainShape ground);
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _vertices[i * 2] = OnGround(ground, centre + direction * radius);
                _vertices[i * 2 + 1] = OnGround(ground, centre + direction * (radius - Width));
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateBounds();
        }

        private Vector3 OnGround(TerrainShape ground, Vector2 point)
        {
            float height = ground != null ? ground.Height(point.x, point.y) : 0f;
            return ring.InverseTransformPoint(new Vector3(point.x, height + Lift, point.y));
        }
    }
}
