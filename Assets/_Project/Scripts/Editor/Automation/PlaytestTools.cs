using BeachTycoon.Core;
using BeachTycoon.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BeachTycoon.EditorTools
{
    /// <summary>Helpers for scripted playtests: real taps through the input path, UI clicks, screenshots.</summary>
    internal static class PlaytestTools
    {
        /// <summary>Taps a world point through TapEvent -> TapInputRouter, exactly like a finger would.</summary>
        public static void TapWorld(Vector3 world)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(world);
            EventBus.Publish(new TapEvent(new Vector2(screen.x, screen.y)));
        }

        /// <summary>Taps an object to select it, then the Upgrade button that floats above it.</summary>
        public static void TapAndUpgrade(Vector3 world)
        {
            TapWorld(world);
            Click(Find<SelectionArrowsView>(), "UpgradeButton");
        }

        /// <summary>Invokes the named Button under a UI component (works on inactive children too).</summary>
        public static void Click(Component owner, string buttonName)
        {
            foreach (Button button in owner.GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                {
                    button.onClick.Invoke();
                    return;
                }
            }

            AutoTestRunner.Fail($"button '{buttonName}' not found under {owner.name}");
        }

        /// <summary>Clicks the build bar button for the product at this index in GameDatabase.products.</summary>
        public static void ClickProduct(int index)
        {
            ProductButtonView[] buttons = Find<BuildBarView>().GetComponentsInChildren<ProductButtonView>(true);
            if (index < buttons.Length)
            {
                buttons[index].GetComponent<Button>().onClick.Invoke();
            }
            else
            {
                AutoTestRunner.Fail($"no product button {index}");
            }
        }

        public static T Find<T>() where T : Component
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(false);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        public static void Capture(string fileName)
        {
            ScreenCapture.CaptureScreenshot("Logs/" + fileName);
            AutoTestRunner.Write("Screenshot Logs/" + fileName);
        }

        /// <summary>Renders the world camera at phone portrait resolution (no HUD) and saves a PNG.</summary>
        public static void CapturePortrait(string fileName, int width = 720, int height = 1280)
        {
            Camera camera = Camera.main;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            System.IO.File.WriteAllBytes("Logs/" + fileName, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            AutoTestRunner.Write("Portrait render Logs/" + fileName);
        }

        /// <summary>Like CapturePortrait, but includes the HUD by rendering overlay canvases through the camera.</summary>
        public static void CapturePortraitWithHud(string fileName, int width = 720, int height = 1280)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>();
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (!canvases[i].isRootCanvas) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = Camera.main;
                canvases[i].planeDistance = Camera.main.nearClipPlane + 0.05f;
            }

            Canvas.ForceUpdateCanvases();
            CapturePortrait(fileName, width, height);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].isRootCanvas) canvases[i].renderMode = modes[i];
            }
        }

        /// <summary>Visible mesh renderers, shadow casters and triangles per scene group (perf report).</summary>
        public static string RendererBreakdown()
        {
            var groups = new System.Collections.Generic.SortedDictionary<string, Vector3Int>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(false))
                {
                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (!renderer.enabled || filter == null || filter.sharedMesh == null) continue;
                    Transform group = renderer.transform;
                    while (group.parent != null && group.parent.parent != null) group = group.parent;
                    long triangles = 0;
                    // A static-batched renderer points at the whole combined mesh, so count it once per group.
                    bool combined = renderer.isPartOfStaticBatch;
                    for (int i = 0; i < filter.sharedMesh.subMeshCount && !(combined && groups.ContainsKey(group.name)); i++)
                    {
                        triangles += filter.sharedMesh.GetIndexCount(i) / 3;
                    }
                    groups.TryGetValue(group.name, out Vector3Int sum);
                    bool casts = renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
                    groups[group.name] = sum + new Vector3Int(1, casts ? 1 : 0, (int)triangles);
                }
            }

            var text = new System.Text.StringBuilder("Renderers by group (count / shadow casters / triangles):");
            foreach (var pair in groups) text.Append($"\n    {pair.Key}: {pair.Value.x} / {pair.Value.y} / {pair.Value.z}");
            return text.ToString();
        }

        /// <summary>Moves the camera to frame a world point at the current angle (test framing only).</summary>
        public static void FocusCamera(Vector3 focus, float distance)
        {
            Camera camera = Camera.main;
            Quaternion rotation = camera.transform.rotation;
            camera.transform.position = focus - rotation * Vector3.forward * distance;
        }
    }
}
