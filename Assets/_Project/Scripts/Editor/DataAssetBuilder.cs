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
            database.world = BuildWorld();
            database.terrain = BuildTerrain();
            database.palette = palette;

            ContentAssetBuilder.Fill(database);
            CommunityAssetBuilder.Fill(database);

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            return database;
        }

        // Version 2 only differs by the two decoration spots beside the footpath, which sat too close to the
        // first house lots for movable placement. Keep any other tweaks and nudge just those two.
        private const int EntranceSpotsVersion = 2;

        private static void MoveEntranceSpots(WorldLayoutConfig world)
        {
            var defaults = ScriptableObject.CreateInstance<WorldLayoutConfig>();
            Vector2[] spots = world.decorationSpots;
            for (int i = 0; spots != null && i < spots.Length && i < defaults.decorationSpots.Length; i++)
            {
                if (Mathf.Approximately(Mathf.Abs(spots[i].x), OldEntranceSpotX)) spots[i] = defaults.decorationSpots[i];
            }

            Object.DestroyImmediate(defaults);
            world.dataVersion = WorldLayoutConfig.CurrentDataVersion;
            EditorUtility.SetDirty(world);
        }

        private const float OldEntranceSpotX = 3.6f;

        // The layout changed between milestones, so older assets are reset to the current defaults.
        private static WorldLayoutConfig BuildWorld()
        {
            var world = LoadOrCreate<WorldLayoutConfig>(ProjectPaths.Config + "/WorldLayoutConfig.asset", out _);
            if (world.dataVersion == EntranceSpotsVersion)
            {
                MoveEntranceSpots(world);
            }
            else if (world.dataVersion < WorldLayoutConfig.CurrentDataVersion)
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

        // One silent slot per sound hook; drop clips in later without touching code.
        private static SoundLibrary BuildSoundLibrary()
        {
            ProjectPaths.EnsureFolder(ProjectPaths.Audio);
            var library = LoadOrCreate<SoundLibrary>(ProjectPaths.Audio + "/SoundLibrary.asset", out bool created);
            if (created)
            {
                var ids = (SoundId[])System.Enum.GetValues(typeof(SoundId));
                library.sounds = new SoundEntry[ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    library.sounds[i] = new SoundEntry { id = ids[i], volume = 1f };
                }

                EditorUtility.SetDirty(library);
            }

            return library;
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
