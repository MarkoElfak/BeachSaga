using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeachTycoon.EditorTools
{
    // Whole-map checks and the real double tap used by the camera playtest.
    internal static partial class CameraPlaytestSteps
    {
        /// <summary>Full zoom-out, centred, with every corner of the land on screen, clear of haze and with shadows.</summary>
        private static void CheckWholeMap(string where, string shot, int width = 720, int height = 1280)
        {
            TopDownCameraController rig = Rig;
            Camera camera = Camera.main;
            Rect area = rig.Area;
            Check(rig.ShowsWholeMap && Mathf.Approximately(rig.Distance, rig.MaxDistance) && Vector2.Distance(Flat(rig.Focus), area.center) < 0.01f,
                $"{where}: full zoom-out ({rig.Distance:0} m) centred on the land");

            bool allIn = true;
            float farthest = 0f;
            string corners = string.Empty;
            for (int i = 0; i < 4; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? area.xMin : area.xMax, 0f, (i & 2) == 0 ? area.yMin : area.yMax);
                Vector3 view = camera.WorldToViewportPoint(corner);
                allIn &= view.z > 0f && view.x >= 0f && view.x <= 1f && view.y >= 0f && view.y <= 1f;
                farthest = Mathf.Max(farthest, view.z);
                corners += $" ({view.x:0.00}, {view.y:0.00})";
            }

            Check(allIn, $"{where}: all four corners of the {area.width:0} x {area.height:0} m land are on screen:{corners}");
            Check(RenderSettings.fogStartDistance > farthest, $"{where}: haze starts at {RenderSettings.fogStartDistance:0} m, beyond the land ({farthest:0} m)");
            float shadows = GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline ? pipeline.shadowDistance : 0f;
            Check(shadows >= rig.Distance && camera.farClipPlane > RenderSettings.fogEndDistance,
                $"{where}: shadows reach {shadows:0} m and drawing {camera.farClipPlane:0} m");
            PlaytestTools.CapturePortrait(shot, width, height);
        }

        // Two real taps on the sea just past the land, through the same path as a finger.
        private static void DoubleTapSea()
        {
            Rect area = Rig.Area;
            var sea = new Vector3(area.center.x, 0f, area.yMax + 5f);
            PlaytestTools.TapWorld(sea);
            PlaytestTools.TapWorld(sea);
            Check(Rig.IsFlying, $"double tap on the sea starts a glide (the tap hit {PlaytestTools.HitName(sea)})");
        }

        private static Vector2 Flat(Vector3 point) => new Vector2(point.x, point.z);
    }
}
