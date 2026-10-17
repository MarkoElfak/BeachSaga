using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace BeachTycoon.EditorTools
{
    /// <summary>Authors the Boot, Beach and Island scenes and registers them in the build scene list.</summary>
    internal static class SceneBuilder
    {
        public static void BuildBootScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameDatabase database = ReloadDatabase();
            var bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            PrimitiveKit.SetReference(bootstrap, "database", database);
            EditorSceneManager.SaveScene(scene, ProjectPaths.BootScene);
        }

        public static void BuildBeachScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameDatabase database = ReloadDatabase();

            Camera camera = CreateCamera(database);
            SceneEnvironment.Build(camera);

            var world = new GameObject("World").AddComponent<BeachWorldBuilder>();
            Transform lotsRoot = new GameObject("Lots").transform;
            var decorations = new GameObject("Decorations").AddComponent<DecorationLayer>();

            var coins = new GameObject("Coins").AddComponent<CoinSpawner>();
            PrimitiveKit.SetReference(coins, "coinPrefab",
                AssetDatabase.LoadAssetAtPath<CoinPickupView>(PlaceholderPrefabBuilder.CoinPath));

            var input = new GameObject("Input");
            var gestures = input.AddComponent<PointerGestureSource>();
            var tapRouter = input.AddComponent<TapInputRouter>();
            PrimitiveKit.SetReference(tapRouter, "worldCamera", camera);

            var visitorViews = new GameObject("Visitors").AddComponent<VisitorViewPool>();
            MoveController mover = PlacementSceneBuilder.Build(database, camera, gestures);

            CreateEventSystem();
            HudBuilder.Build(camera, coins);

            var root = new GameObject("BeachSceneRoot").AddComponent<BeachSceneRoot>();
            PrimitiveKit.SetReference(root, "worldBuilder", world);
            PrimitiveKit.SetReference(root, "cameraController", camera.GetComponent<TopDownCameraController>());
            PrimitiveKit.SetReference(root, "gestureSource", gestures);
            PrimitiveKit.SetReference(root, "tapRouter", tapRouter);
            PrimitiveKit.SetReference(root, "coinSpawner", coins);
            PrimitiveKit.SetReference(root, "visitorViews", visitorViews);
            PrimitiveKit.SetReference(root, "decorations", decorations);
            PrimitiveKit.SetReference(root, "lotPrefab",
                AssetDatabase.LoadAssetAtPath<LotView>(PlaceholderPrefabBuilder.LotPath));
            PrimitiveKit.SetReference(root, "lotsRoot", lotsRoot);
            PrimitiveKit.SetReference(root, "moveController", mover);

            EditorSceneManager.SaveScene(scene, ProjectPaths.BeachScene);
        }

        /// <summary>Read-only visit scene: world, props, crowd, camera and input, and the island HUD.</summary>
        public static void BuildIslandScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameDatabase database = ReloadDatabase();

            Camera camera = CreateCamera(database);
            SceneEnvironment.Build(camera);
            var world = new GameObject("World").AddComponent<BeachWorldBuilder>();
            var props = new GameObject("IslandProps").AddComponent<IslandPropSpawner>();
            var crowd = new GameObject("Visitors").AddComponent<VisitCrowd>();

            var input = new GameObject("Input");
            var gestures = input.AddComponent<PointerGestureSource>();
            var tapRouter = input.AddComponent<TapInputRouter>();
            PrimitiveKit.SetReference(tapRouter, "worldCamera", camera);

            CreateEventSystem();
            OnlineUiBuilder.BuildIslandHud();

            var root = new GameObject("IslandSceneRoot").AddComponent<IslandSceneRoot>();
            PrimitiveKit.SetReference(root, "worldBuilder", world);
            PrimitiveKit.SetReference(root, "cameraController", camera.GetComponent<TopDownCameraController>());
            PrimitiveKit.SetReference(root, "gestureSource", gestures);
            PrimitiveKit.SetReference(root, "tapRouter", tapRouter);
            PrimitiveKit.SetReference(root, "props", props);
            PrimitiveKit.SetReference(root, "crowd", crowd);

            EditorSceneManager.SaveScene(scene, ProjectPaths.IslandScene);
        }

        public static void RegisterBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ProjectPaths.BootScene, true),
                new EditorBuildSettingsScene(ProjectPaths.BeachScene, true),
                new EditorBuildSettingsScene(ProjectPaths.IslandScene, true),
            };
        }

        // NewScene(Single) unloads unreferenced assets, which turns earlier references into dead objects.
        private static GameDatabase ReloadDatabase()
        {
            return AssetDatabase.LoadAssetAtPath<GameDatabase>(DataAssetBuilder.DatabasePath);
        }

        private static Camera CreateCamera(GameDatabase database)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            go.AddComponent<TopDownCameraController>();

            // Edit-time preview only; TopDownCameraController sets the real pose at runtime.
            CameraConfig config = database.cameraConfig;
            camera.fieldOfView = config.fieldOfView;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = database.palette.GetColor(database.world.seaPaletteKey);
            Quaternion rotation = Quaternion.Euler(config.pitchDegrees, config.yawDegrees, 0f);
            go.transform.SetPositionAndRotation(new Vector3(0f, 0f, -2f) - rotation * Vector3.forward * config.startDistance, rotation);
            return camera;
        }

        private static void CreateEventSystem()
        {
            // No actions assigned: the module picks up the default UI actions in OnEnable at runtime. Assigning
            // them here works only once per editor session (the shared default asset dies with the first scene).
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
