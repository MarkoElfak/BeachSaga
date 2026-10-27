using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    // Islands rise from the sea as rounded squares around each island piece (its land plus a beach); boardwalk
    // decks sit above the water so visitors can walk out to them.
    public sealed partial class TerrainShape
    {
        private const float CoastNoiseShare = 0.5f; // island coasts wobble less than the mainland's

        private readonly List<Island> _islands = new List<Island>();
        private readonly List<Rect> _decks = new List<Rect>();
        private float _islandElevation;
        private float _deckHeight;

        private struct Island
        {
            public Vector2 Centre;
            public Vector2 Half;
        }

        /// <summary>Height a visitor stands at: the ground, or a boardwalk deck where one crosses the water.</summary>
        public float WalkHeight(float x, float z)
        {
            float h = Height(x, z);
            var point = new Vector2(x, z);
            for (int i = 0; i < _decks.Count; i++)
            {
                if (_decks[i].Contains(point)) return Mathf.Max(h, _deckHeight);
            }

            return h;
        }

        /// <summary>1 on an island's dry land, 0 off it; 'core' gives its grassy middle instead.</summary>
        public float IslandLand(float x, float z, bool core)
        {
            float weight = 0f;
            for (int i = 0; i < _islands.Count; i++)
            {
                float d = IslandDistance(_islands[i], x, z);
                weight = Mathf.Max(weight, core ? 1f - Smooth(0.42f, 0.62f, d) : 1f - Smooth(0.9f, 1f, d));
            }

            return weight;
        }

        private void AddIslands(MapConfig map)
        {
            if (!LandMap.HasPieces(map)) return;
            _islandElevation = map.islandElevation;
            _deckHeight = _config.waterLevel + map.boardwalkHeight;
            foreach (LandPiece piece in map.pieces)
            {
                if (!piece.island) continue;
                _islands.Add(new Island { Centre = piece.area.center, Half = piece.area.size * 0.5f + Vector2.one * map.islandBeach });
                if (LandMap.HasBoardwalk(piece)) _decks.Add(LandMap.BoardwalkArea(map, piece));
            }
        }

        private Rect WithIslands(Rect rect)
        {
            float reach = _config.coastFalloff + 3f;
            foreach (Island island in _islands)
            {
                Vector2 half = island.Half + Vector2.one * reach;
                rect = LandMap.Encapsulate(rect, new Rect(island.Centre - half, half * 2f));
            }

            return rect;
        }

        private float IslandHeight(float x, float z)
        {
            float h = _config.seaFloorHeight;
            for (int i = 0; i < _islands.Count; i++)
            {
                Island island = _islands[i];
                float reach = _config.coastFalloff + 1f;
                if (Mathf.Abs(x - island.Centre.x) > island.Half.x + reach || Mathf.Abs(z - island.Centre.y) > island.Half.y + reach) continue;
                float noise = (Mathf.PerlinNoise(x * _config.coastNoiseScale + _noiseOffset, z * _config.coastNoiseScale) - 0.5f)
                    * 2f * _config.coastNoiseAmplitude * CoastNoiseShare;
                float metres = (IslandDistance(island, x, z) - 1f) * Mathf.Min(island.Half.x, island.Half.y) + noise;
                h = Mathf.Max(h, Mathf.Lerp(_islandElevation, _config.seaFloorHeight, Smooth(-0.5f, _config.coastFalloff, metres)));
            }

            return h;
        }

        // A squared-off ellipse (power 4): below 1 on the land, 1 at the coast.
        private static float IslandDistance(in Island island, float x, float z)
        {
            float dx = (x - island.Centre.x) / island.Half.x;
            float dz = (z - island.Centre.y) / island.Half.y;
            dx *= dx;
            dz *= dz;
            return Mathf.Sqrt(Mathf.Sqrt(dx * dx + dz * dz));
        }
    }
}
