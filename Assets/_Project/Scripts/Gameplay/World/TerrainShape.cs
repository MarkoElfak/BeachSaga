using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Height function of the island, shared by the terrain mesh, the water shore mask, scatter placement and
    /// anything that needs to stand on the ground. Heights blend between band centre elevations, then a noisy
    /// coast drops into the sea, dunes ripple the sand and occupied lots are flattened to height 0.
    /// The flat areas follow the buildings: SetFlatRects is called again whenever one is moved. Islands from the
    /// MapConfig rise out of the sea on their own (TerrainShape.Islands), with boardwalk decks to walk on.
    /// </summary>
    public sealed partial class TerrainShape
    {
        private readonly WorldLayoutConfig _layout;
        private readonly TerrainConfig _config;
        private readonly List<Vector2> _profile = new List<Vector2>(); // (z, height), ascending z
        private readonly List<Rect> _flatRects = new List<Rect>();
        private readonly float _noiseOffset;
        private readonly float _shoreStartZ;

        /// <param name="lotFlats">Footprints (XZ) to flatten, normally from PlacementService.GetFlatRects.</param>
        /// <param name="map">Adds the map's islands and boardwalks (optional).</param>
        public TerrainShape(WorldLayoutConfig layout, TerrainConfig config, IReadOnlyList<Rect> lotFlats, MapConfig map = null)
        {
            _layout = layout;
            _config = config;
            _noiseOffset = config.noiseSeed * 17.31f;
            AddIslands(map);

            float top = layout.landTopZ;
            float z = top;
            _shoreStartZ = top;
            foreach (BandSpec band in layout.bands)
            {
                _profile.Add(new Vector2(z - band.depth * 0.5f, band.elevation));
                if (band.elevation >= config.waterLevel && _shoreStartZ >= top) _shoreStartZ = z;
                z -= band.depth;
            }

            BottomZ = z;
            float lastElevation = layout.bands.Length > 0 ? layout.bands[layout.bands.Length - 1].elevation : 0f;
            _profile.Add(new Vector2(z, lastElevation)); // keep the last band at full height to its edge
            _profile.Add(new Vector2(top + config.coastFalloff, config.seaFloorHeight));
            _profile.Add(new Vector2(z - config.coastFalloff, config.seaFloorHeight));
            _profile.Sort((a, b) => a.x.CompareTo(b.x));

            float halfWidth = layout.width * 0.5f;
            LandRect = Rect.MinMaxRect(-halfWidth, BottomZ, halfWidth, top);
            TerrainRect = WithIslands(Rect.MinMaxRect(-halfWidth - config.sideMargin, BottomZ - config.backMargin,
                halfWidth + config.sideMargin, top + config.seaMargin));
            SetFlatRects(lotFlats);
        }

        /// <summary>The playable island (bands) in XZ.</summary>
        public Rect LandRect { get; }

        /// <summary>Area covered by the terrain mesh, including the sea margins.</summary>
        public Rect TerrainRect { get; }

        public float BottomZ { get; }
        public float WaterLevel => _config.waterLevel;

        public float Height(float x, float z) => Height(x, z, true);

        public float Height(float x, float z, bool flattenLots)
        {
            // Wavy shoreline: shift the sea-side profile a little along x.
            float wave = (Mathf.PerlinNoise(x * 0.06f + _noiseOffset, 3.1f) - 0.5f) * 1.4f;
            float shoreMask = Mathf.Clamp01((z - _shoreStartZ + 4f) * 0.25f);
            float backWave = (Mathf.PerlinNoise(x * _config.coastNoiseScale, _noiseOffset) - 0.5f) * 2f * _config.coastNoiseAmplitude;
            float zEff = z + wave * shoreMask + (z < BottomZ + 1f ? backWave : 0f);
            float h = Profile(zEff);

            // Noisy left and right coasts.
            float edgeNoise = (Mathf.PerlinNoise(z * _config.coastNoiseScale + _noiseOffset, 7.7f) - 0.5f) * 2f;
            float over = Mathf.Abs(x) - (_layout.width * 0.5f + edgeNoise * _config.coastNoiseAmplitude);
            h = Mathf.Lerp(h, _config.seaFloorHeight, Smooth(-0.5f, _config.coastFalloff, over));

            h = Mathf.Max(h, IslandHeight(x, z));

            // Dunes only on dry land.
            float land = Smooth(_config.waterLevel + 0.02f, _config.waterLevel + 0.12f, h);
            float dune = Mathf.PerlinNoise(x * _config.duneScale + _noiseOffset, z * _config.duneScale * 1.6f);
            h += (dune - 0.45f) * _config.duneAmplitude * land;

            return flattenLots ? Flatten(x, z, h) : h;
        }

        /// <summary>World position on the ground at (x, z).</summary>
        public Vector3 Ground(float x, float z) => new Vector3(x, Height(x, z), z);

        public float WaterDepth(float x, float z) => _config.waterLevel - Height(x, z, false);

        private float Flatten(float x, float z, float h)
        {
            float margin = _config.lotFlattenMargin;
            for (int i = 0; i < _flatRects.Count; i++)
            {
                Rect r = _flatRects[i];
                float dx = Mathf.Max(r.xMin - x, 0f, x - r.xMax);
                float dz = Mathf.Max(r.yMin - z, 0f, z - r.yMax);
                if (dx > margin || dz > margin) continue;
                float weight = 1f - Smooth(0f, margin, Mathf.Sqrt(dx * dx + dz * dz));
                h = Mathf.Lerp(h, 0f, weight);
            }

            return h;
        }

        private float Profile(float z)
        {
            if (z <= _profile[0].x) return _profile[0].y;
            for (int i = 1; i < _profile.Count; i++)
            {
                Vector2 b = _profile[i];
                if (z > b.x) continue;
                Vector2 a = _profile[i - 1];
                return Mathf.Lerp(a.y, b.y, Smooth(a.x, b.x, z));
            }

            return _profile[_profile.Count - 1].y;
        }

        /// <summary>Replaces the flattened lot areas; the footpath always stays flat and walkable.</summary>
        public void SetFlatRects(IReadOnlyList<Rect> lotFlats)
        {
            _flatRects.Clear();
            if (lotFlats != null)
            {
                for (int i = 0; i < lotFlats.Count; i++) _flatRects.Add(lotFlats[i]);
            }

            _flatRects.Add(_config.path);
        }

        private static float Smooth(float edge0, float edge1, float value)
        {
            float t = Mathf.Clamp01((value - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }
    }
}
