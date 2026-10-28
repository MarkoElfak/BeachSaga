using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Phone test builds only (development builds, never the Editor or a release): prints the graphics device,
    /// whether the world shaders run on it, the frame rate with the worst frame of the last second, the visitors
    /// and the picture quality, and the latest warnings and errors, so a screenshot says how the phone is doing.
    /// "Fill beach" brings the whole visitor pool at once, the busiest the beach can get.
    /// </summary>
    public sealed class DevBuildOverlay : MonoBehaviour
    {
        private const int MaxLines = 8;

        private readonly Queue<string> _lines = new Queue<string>();
        private string _header;
        private string _stats = string.Empty;
        private GUIStyle _style;
        private GUIStyle _button;
        private float _second;
        private int _frames;
        private float _worst;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartInTestBuilds()
        {
            if (Application.isEditor || !Debug.isDebugBuild) return;
            var host = new GameObject("DevBuildOverlay");
            DontDestroyOnLoad(host);
            host.AddComponent<DevBuildOverlay>();
        }

        private void Awake()
        {
            Application.logMessageReceived += OnLog;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader water = Shader.Find("BeachTycoon/Water");
            _header = $"{SystemInfo.graphicsDeviceType} {SystemInfo.graphicsDeviceVersion}\n{SystemInfo.graphicsDeviceName}\n" +
                $"Lit: {(lit == null ? "missing" : lit.isSupported ? "ok" : "NOT SUPPORTED")}, Water: {(water == null ? "missing" : water.isSupported ? "ok" : "NOT SUPPORTED")}, " +
                $"quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}";
        }

        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        // Frame rate over the last second, written once a second.
        private void Update()
        {
            float frame = Time.unscaledDeltaTime;
            _second += frame;
            _frames++;
            if (frame > _worst) _worst = frame;
            if (_second < 1f) return;

            int visitors = ServiceLocator.TryGet(out VisitorService crowd) ? crowd.ActiveCount : 0;
            int steps = ServiceLocator.TryGet(out FrameRateGuard guard) ? guard.Steps : 0;
            _stats = $"{_frames / _second:0} fps, worst frame {_worst * 1000f:0} ms, {visitors} visitors, " +
                $"render scale {FrameRateGuard.RenderScale:0.00} (lowered {steps}x)";
            _second = 0f;
            _frames = 0;
            _worst = 0f;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            _lines.Enqueue($"[{type}] {(message.Length > 160 ? message.Substring(0, 160) : message)}");
            while (_lines.Count > MaxLines) _lines.Dequeue();
        }

        private void OnGUI()
        {
            int font = Mathf.RoundToInt(Screen.height / 70f);
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = font, wordWrap = true };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = font };
            var area = new Rect(10f, Screen.height * 0.3f, Screen.width - 20f, Screen.height * 0.3f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(area, _header + "\n" + _stats + "\n" + string.Join("\n", _lines), _style);

            var fill = new Rect(area.x, area.yMax + 8f, Screen.width * 0.35f, font * 2.5f);
            if (GUI.Button(fill, "Fill beach", _button) && ServiceLocator.TryGet(out VisitorService visitors))
            {
                while (visitors.TrySpawn()) { }
            }
        }
    }
}
