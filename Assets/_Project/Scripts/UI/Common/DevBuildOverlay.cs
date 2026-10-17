using System.Collections.Generic;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Phone test builds only (development builds, never the Editor or a release): prints the graphics device,
    /// whether the world shaders run on it, and the latest warnings and errors, so a screenshot says what failed.
    /// </summary>
    public sealed class DevBuildOverlay : MonoBehaviour
    {
        private const int MaxLines = 8;

        private readonly Queue<string> _lines = new Queue<string>();
        private string _header;
        private GUIStyle _style;

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

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            _lines.Enqueue($"[{type}] {(message.Length > 160 ? message.Substring(0, 160) : message)}");
            while (_lines.Count > MaxLines) _lines.Dequeue();
        }

        private void OnGUI()
        {
            _style ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height / 70f), wordWrap = true };
            var area = new Rect(10f, Screen.height * 0.3f, Screen.width - 20f, Screen.height * 0.45f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(area, _header + "\n" + string.Join("\n", _lines), _style);
        }
    }
}
