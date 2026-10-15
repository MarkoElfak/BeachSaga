using BeachTycoon.Data;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>
    /// Creates the config assets and the GameDatabase that ties everything together. Values are written
    /// only when an asset is first created, so tuning done in the Inspector survives re-running Setup.
    /// </summary>
    internal static class DataAssetBuilder
    {
        public const string DatabasePath = ProjectPaths.Data + "/GameDatabase.asset";

        public static GameDatabase BuildOrUpdate(PaletteDefinition palette)
        {
            ProjectPaths.EnsureFolder(ProjectPaths.Config);

            var database = LoadOrCreate<GameDatabase>(DatabasePath, out _);
            database.economy = LoadOrCreate<EconomyConfig>(ProjectPaths.Config + "/EconomyConfig.asset", out _);
            database.cameraConfig = LoadOrCreate<CameraConfig>(ProjectPaths.Config + "/CameraConfig.asset", out _);
            database.input = LoadOrCreate<InputConfig>(ProjectPaths.Config + "/InputConfig.asset", out _);
            database.presentation = LoadOrCreate<PresentationConfig>(ProjectPaths.Config + "/PresentationConfig.asset", out _);
            database.visitors = LoadOrCreate<VisitorConfig>(ProjectPaths.Config + "/VisitorConfig.asset", out _);
            database.world = BuildWorld();
            database.terrain = BuildTerrain();
            database.palette = palette;

            ContentAssetBuilder.Fill(database);
            CommunityAssetBuilder.Fill(database);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return database;
        }

        // The layout changed between milestones, so older assets are reset to the current defaults.
        private static WorldLayoutConfig BuildWorld()
        {
            var world = LoadOrCreate<WorldLayoutConfig>(ProjectPaths.Config + "/WorldLayoutConfig.asset", out _);
            if (world.dataVersion < WorldLayoutConfig.CurrentDataVersion)
            {
                var defaults = ScriptableObject.CreateInstance<WorldLayoutConfig>();
                EditorUtility.CopySerialized(defaults, world);
                Object.DestroyImmediate(defaults);
                world.dataVersion = WorldLayoutConfig.CurrentDataVersion;
            }

            if (world.palmPrefab == null)
            {
                world.palmPrefab = PlaceholderPrefabBuilder.Load<GameObject>(PlaceholderPrefabBuilder.PalmPath);
            }

            EditorUtility.SetDirty(world);
            return world;
        }

        private static TerrainConfig BuildTerrain()
        {
            var terrain = LoadOrCreate<TerrainConfig>(ProjectPaths.Config + "/TerrainConfig.asset", out _);
            terrain.terrainMaterial = RenderingAssets.TerrainMaterial();
            terrain.waterMaterial = RenderingAssets.WaterMaterial();
            terrain.rockMaterial = RenderingAssets.RockMaterial();
            terrain.grassTuftMaterial = RenderingAssets.GrassTuftMaterial();
            EditorUtility.SetDirty(terrain);
            return terrain;
        }

        public static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = asset == null;
            if (created)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }
    }
}
