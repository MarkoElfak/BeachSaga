using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Builds the island at runtime from WorldLayoutConfig + TerrainConfig: a height-field terrain with a baked
    /// texture, a water surface with shore foam, palms and instanced rocks and grass. Editing the configs
    /// reshapes the beach without code changes.
    /// </summary>
    public sealed class BeachWorldBuilder : MonoBehaviour
    {
        private const int MaxPlacementAttempts = 40;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");

        [SerializeField] private Transform worldRoot = null;

        private WorldLayoutConfig _layout;
        private TerrainConfig _terrain;

        public TerrainShape Shape { get; private set; }

        /// <summary>Builds the world and returns the playable land rectangle in XZ (x = X, y = Z).</summary>
        public Rect Build(GameDatabase database)
        {
            Transform root = worldRoot != null ? worldRoot : transform;
            WorldLayoutConfig layout = database.world;
            TerrainConfig config = database.terrain;
            _layout = layout;
            _terrain = config;
            Shape = new TerrainShape(layout, config);

            TerrainTextureBaker.Bake(Shape, layout, config, database.palette, out Texture2D albedo, out Texture2D normal);
            var terrainMaterial = new Material(config.terrainMaterial) { name = "Terrain (runtime)" };
            terrainMaterial.SetTexture(BaseMapId, albedo);
            terrainMaterial.SetTexture(BumpMapId, normal);
            CreateRenderer(root, "Terrain", TerrainMeshBuilder.BuildTerrain(Shape, config.meshCellSize), terrainMaterial, false, true);

            Mesh water = TerrainMeshBuilder.BuildWater(Shape, config.waterCellSize, config.shoreDepthRange,
                config.deepDepthRange, config.waterExtent);
            CreateRenderer(root, "Water", water, config.waterMaterial, false, false);

            var random = new System.Random(layout.palmSeed);
            PlacePalms(root, layout, random);
            Scatter(root, "Rocks", ProceduralMeshes.Rock(layout.palmSeed), config.rockMaterial, config.rockCount,
                config.rockScaleRange, random, true, IsRockSpot);
            Scatter(root, "GrassTufts", ProceduralMeshes.GrassTuft(layout.palmSeed), config.grassTuftMaterial,
                config.grassTuftCount, config.tuftScaleRange, random, false, IsTuftSpot);
            return Shape.LandRect;
        }

        private static void CreateRenderer(Transform parent, string objectName, Mesh mesh, Material material,
            bool castShadows, bool receiveShadows)
        {
            var go = new GameObject(objectName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
        }

        private void PlacePalms(Transform parent, WorldLayoutConfig layout, System.Random random)
        {
            if (layout.palmPrefab == null) return;
            Rect land = Shape.LandRect;
            for (int i = 0; i < layout.palmCount; i++)
            {
                if (!TryFindSpot(random, land, layout.palmEdgeMargin, IsPalmSpot, out Vector3 p)) continue;
                GameObject palm = Instantiate(layout.palmPrefab, parent, false);
                float scale = Mathf.Lerp(layout.palmScaleRange.x, layout.palmScaleRange.y, (float)random.NextDouble());
                palm.transform.SetLocalPositionAndRotation(p, Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f));
                palm.transform.localScale = Vector3.one * scale;
            }
        }

        private void Scatter(Transform parent, string objectName, Mesh mesh, Material material, int count,
            Vector2 scaleRange, System.Random random, bool castShadows, System.Func<Vector3, bool> accept)
        {
            if (material == null || count <= 0) return;
            var matrices = new List<Matrix4x4>(count);
            Rect area = Shape.TerrainRect;
            for (int i = 0; i < count; i++)
            {
                if (!TryFindSpot(random, area, 0f, accept, out Vector3 p)) continue;
                float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)random.NextDouble());
                var rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                matrices.Add(Matrix4x4.TRS(p, rotation, Vector3.one * scale));
            }

            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.AddComponent<ScatterRenderer>().Setup(mesh, material, matrices, castShadows);
        }

        private bool TryFindSpot(System.Random random, Rect area, float margin, System.Func<Vector3, bool> accept, out Vector3 point)
        {
            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                float x = Mathf.Lerp(area.xMin + margin, area.xMax - margin, (float)random.NextDouble());
                float z = Mathf.Lerp(area.yMin + margin, area.yMax - margin, (float)random.NextDouble());
                point = Shape.Ground(x, z);
                if (accept(point)) return true;
            }

            point = default;
            return false;
        }

        private bool IsPalmSpot(Vector3 p)
        {
            WorldLayoutConfig layout = _layout;
            float grassTop = Shape.BottomZ + layout.bands[Mathf.Min(layout.palmBandIndex, layout.bands.Length - 1)].depth;
            bool onGrass = p.z < grassTop && p.y > Shape.WaterLevel + 0.05f;
            return onGrass && !NearLotOrPath(p, layout.palmClearance);
        }

        private bool IsRockSpot(Vector3 p)
        {
            float aboveWater = p.y - Shape.WaterLevel;
            bool shoreline = aboveWater > -0.35f && aboveWater < 0.12f;
            return shoreline && !_layout.relaxArea.Contains(new Vector2(p.x, p.z)) && !NearLotOrPath(p, 1f);
        }

        private bool IsTuftSpot(Vector3 p)
        {
            bool land = p.y > Shape.WaterLevel + 0.08f;
            bool grassOrEdge = p.z < Shape.BottomZ + 6.5f || Mathf.Abs(p.x) > _layout.width * 0.5f - 1.5f;
            return land && grassOrEdge && !NearLotOrPath(p, 0.9f);
        }

        private bool NearLotOrPath(Vector3 p, float clearance)
        {
            WorldLayoutConfig layout = _layout;
            var point = new Vector2(p.x, p.z);
            for (int i = 0; i < layout.lotPositions.Length; i++)
            {
                Rect footprint = i == BuildingManager.MainLotIndex ? layout.mainFootprint : layout.houseFootprint;
                if (Expand(new Rect(layout.lotPositions[i] + footprint.position, footprint.size), clearance).Contains(point)) return true;
            }

            return Expand(_terrain.path, clearance).Contains(point);
        }

        private static Rect Expand(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);
    }
}
