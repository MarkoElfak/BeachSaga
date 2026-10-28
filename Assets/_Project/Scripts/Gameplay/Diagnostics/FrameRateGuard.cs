using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Keeps play smooth on slower phones: while the phone can't hold the frame rate (FrameRateGovernor), it lowers
    /// the render scale a step at a time, then turns shadows off. Builds only, for this session only (the Editor's
    /// settings asset is never changed).
    /// </summary>
    public sealed class FrameRateGuard : ITickable
    {
        private readonly PresentationConfig _config;
        private readonly FrameRateGovernor _governor;
        private readonly bool _active;

        public FrameRateGuard(PresentationConfig config)
        {
            _config = config;
            _governor = new FrameRateGovernor(config.targetFrameRate, config.slowFrameFactor, config.qualityCheckSeconds, config.slowFrameShare);
            _active = config.adaptiveQuality && !Application.isEditor;
        }

        /// <summary>How many times the picture was lowered this session.</summary>
        public int Steps { get; private set; }

        public bool AtLowest { get; private set; }

        /// <summary>The render scale in use now (1 without URP).</summary>
        public static float RenderScale =>
            GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline ? pipeline.renderScale : 1f;

        public void Tick(float deltaTime)
        {
            if (!_active || AtLowest || !_governor.Sample(Time.unscaledDeltaTime)) return;
            StepDown();
        }

        private void StepDown()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline))
            {
                AtLowest = true;
                return;
            }

            if (pipeline.renderScale - _config.renderScaleStep >= _config.minRenderScale - 0.001f)
            {
                pipeline.renderScale -= _config.renderScaleStep;
            }
            else if (pipeline.shadowDistance > 0f)
            {
                pipeline.shadowDistance = 0f;
                AtLowest = true;
            }
            else
            {
                AtLowest = true;
                return;
            }

            Steps++;
        }
    }
}
