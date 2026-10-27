using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeachTycoon.Gameplay
{
    /// <summary>A sign's text, set by the world (the sign prefab's view lives in the UI assembly).</summary>
    public interface ISignLabel
    {
        void SetText(string text);
    }

    /// <summary>
    /// Fog over one locked piece of the map: stacked drifting layers that thin out at the edges, an
    /// "Opens at ..." sign, and a tap area. Reveal() dissolves the fog, lifts it and sinks the sign.
    /// </summary>
    public sealed class LockedLandView : MonoBehaviour, ITappable
    {
        private const float TapHeight = 0.6f; // low, so taps on open land just beside the fog still reach the ground
        private const float RevealLift = 1.5f;
        private const float SignSink = 3.5f;
        private static readonly int RevealId = Shader.PropertyToID("_Reveal");
        private static readonly int LayerId = Shader.PropertyToID("_Layer");

        private readonly List<Material> _materials = new List<Material>();
        private readonly List<Transform> _layers = new List<Transform>();
        private readonly List<float> _layerHeights = new List<float>();
        private Mesh _mesh;
        private Transform _sign;
        private Vector3 _signStart;
        private BoxCollider _tapArea;
        private float _revealTime;
        private float _revealSeconds = 1f;

        public int Piece { get; private set; }
        public Rect Area { get; private set; }
        public string SignText { get; private set; }
        public bool IsOpening { get; private set; }
        public int LayerCount => _layers.Count;

        public void Build(GameDatabase database, int piece, TerrainShape ground)
        {
            MapConfig map = database.map;
            LandPiece land = map.pieces[piece];
            Piece = piece;
            Area = LandMap.FogArea(map, piece);
            _mesh = FogMesh.Build(Area, map.fogFadeInside, map.fogFadeOutside, map.fogCellSize);
            for (int layer = 0; layer < map.fogLayers; layer++) AddLayer(map, layer);

            _tapArea = gameObject.AddComponent<BoxCollider>();
            _tapArea.isTrigger = true;
            _tapArea.center = new Vector3(Area.center.x, TapHeight * 0.5f, Area.center.y);
            _tapArea.size = new Vector3(Area.width, TapHeight, Area.height);
            enabled = false; // Update only runs while the fog rolls away

            TierDefinition tier = database.GetTier(land.unlockTier);
            SignText = $"{land.displayName}\nOpens at {(tier != null ? tier.displayName : $"level {land.unlockTier}")}";
            if (map.signPrefab == null) return;
            _sign = Instantiate(map.signPrefab, transform, false).transform;
            _signStart = ground != null ? ground.Ground(land.signPosition.x, land.signPosition.y) : new Vector3(land.signPosition.x, 0f, land.signPosition.y);
            _sign.localPosition = _signStart;
            _sign.GetComponent<ISignLabel>()?.SetText(SignText);
        }

        public void OnTapped() => EventBus.Publish(new LockedLandTapped(Piece));

        /// <summary>The land opened: the fog dissolves and rises, the sign sinks, then everything switches off.</summary>
        public void Reveal(float seconds)
        {
            if (IsOpening) return;
            IsOpening = true;
            _revealSeconds = Mathf.Max(0.05f, seconds);
            _revealTime = 0f;
            _tapArea.enabled = false;
            enabled = true;
        }

        private void Update()
        {
            _revealTime += Time.deltaTime;
            float t = Mathf.Clamp01(_revealTime / _revealSeconds);
            for (int i = 0; i < _materials.Count; i++) _materials[i].SetFloat(RevealId, t);
            for (int i = 0; i < _layers.Count; i++)
            {
                Vector3 p = _layers[i].localPosition;
                _layers[i].localPosition = new Vector3(p.x, _layerHeights[i] + t * RevealLift, p.z);
            }

            if (_sign != null) _sign.localPosition = _signStart + Vector3.down * (t * t * SignSink);
            if (t >= 1f) gameObject.SetActive(false);
        }

        private void AddLayer(MapConfig map, int layer)
        {
            var go = new GameObject($"FogLayer{layer}", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            float height = map.fogHeight + layer * map.fogLayerGap;
            go.transform.localPosition = new Vector3(0f, height, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = _mesh;
            var material = new Material(map.fogMaterial) { name = $"Fog {Piece}.{layer}" };
            material.SetFloat(LayerId, Piece * 3.1f + layer * 1.7f);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _materials.Add(material);
            _layers.Add(go.transform);
            _layerHeights.Add(height);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _materials.Count; i++) Destroy(_materials[i]);
            if (_mesh != null) Destroy(_mesh);
        }
    }
}
