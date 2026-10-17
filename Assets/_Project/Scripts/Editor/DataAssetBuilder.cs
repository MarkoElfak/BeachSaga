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
            database.monetization = LoadOrCreate<MonetizationConfig>(ProjectPaths.Config + "/MonetizationConfig.asset", out _);
            database.tutorial = LoadOrCreate<TutorialConfig>(ProjectPaths.Config + "/TutorialConfig.asset", out _);
            database.sounds = BuildSoundLibrary();
            database.placement = LoadOrCreate<PlacementConfig>(ProjectPaths.Config + "/PlacementConfig.asset", out _);
            database.dailyRewards = LoadOrCreate<DailyRewardConfig>(ProjectPaths.Config + "/DailyRewardConfig.asset", out _);
            database.online = LoadOrCreate<OnlineConfig>(ProjectPaths.Config + "/OnlineConfig.asset", out _);
            database.world = BuildWorld();
            database.terrain = BuildTerrain(palette);
            database.palette = palette;

            ContentAssetBuilder.Fill(database);
            CommunityAssetBuilder.Fill(database);
            AchievementAssetBuilder.Fill(database);
            EventAssetBuilder.Fill(database);
            MiniGameAssetBuilder.Fill(database);
            FootprintMeasure.Bake(database);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return database;
        }

        // The layout changed between milestones (M4.1: a bigger island), so older assets are reset to the defaults.
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

            // CopySerialized also copies the empty default name, which Unity warns about on every load.
            if (world.name != "WorldLayoutConfig")
            {
                world.name = "WorldLayoutConfig";
            }

            if (world.palmPrefab == null)
            {
                world.palmPrefab = PlaceholderPrefabBuilder.Load<GameObject>(PlaceholderPrefabBuilder.PalmPath);
            }

            EditorUtility.SetDirty(world);
            return world;
        }

        // A slot per sound hook, filled with generated placeholder clips; assign your own clips any time.
        private static SoundLibrary BuildSoundLibrary()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.Audio);
            var library = LoadOrCreate<SoundLibrary>(ProjectPaths.Audio + "/SoundLibrary.asset", out _);
            AudioAssetBuilder.Fill(library);
            return library;
        }

        private static TerrainConfig BuildTerrain(PaletteDefinition palette)
        {
            var terrain = LoadOrCreate<TerrainConfig>(ProjectPaths.Config + "/TerrainConfig.asset", out _);
            if (terrain.dataVersion < TerrainConfig.CurrentDataVersion)
            {
                // The island grew: longer footpath and more scenery. Other terrain tuning is kept.
                var defaults = ScriptableObject.CreateInstance<TerrainConfig>();
                terrain.path = defaults.path;
                terrain.rockCount = defaults.rockCount;
                terrain.grassTuftCount = defaults.grassTuftCount;
                Object.DestroyImmediate(defaults);
                terrain.dataVersion = TerrainConfig.CurrentDataVersion;
            }

            terrain.terrainMaterial = RenderingAssets.TerrainMaterial();
            terrain.waterMaterial = RenderingAssets.WaterMaterial();
            terrain.rockMaterial = RenderingAssets.RockMaterial();
            terrain.grassTuftMaterial = RenderingAssets.GrassTuftMaterial();
            terrain.fenceMesh = LowPolyPrimitives.For(PrimitiveType.Cube);
            terrain.fencePostMaterial = palette.GetMaterial("fencePost");
            terrain.fenceRopeMaterial = palette.GetMaterial("fenceRope");
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
