using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Visual-only tuning: frame rate, pools and small animations.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Presentation", fileName = "PresentationConfig")]
    public sealed class PresentationConfig : ScriptableObject
    {
        [Min(15)] public int targetFrameRate = 60;

        [Header("Smooth on slower phones (builds only)")]
        [Tooltip("While the phone can't hold the frame rate, lower the render scale a step at a time, then turn shadows off.")]
        public bool adaptiveQuality = true;
        [Range(0.3f, 1f)] public float minRenderScale = 0.6f;
        [Range(0.05f, 0.5f)] public float renderScaleStep = 0.1f;
        [Tooltip("A frame is slow when it takes this many times the target frame's time (1.2 at 60 fps: under 50 fps).")]
        [Min(1f)] public float slowFrameFactor = 1.2f;
        [Min(1f)] public float qualityCheckSeconds = 5f;
        [Tooltip("The share of slow frames in a check that lowers the picture one step.")]
        [Range(0.1f, 1f)] public float slowFrameShare = 0.5f;

        [Header("Coin pickups")]
        [Min(1)] public int coinPoolPrewarm = 12;
        [Min(1)] public int coinPoolMaxSize = 64;
        public float coinSpinDegreesPerSecond = 180f;
        [Min(0f)] public float coinBobHeight = 0.15f;
        [Min(0f)] public float coinBobSpeed = 3f;
        [Tooltip("Coins are placed on a small ring around the stand's coin anchor.")]
        [Min(0f)] public float coinRingRadius = 0.7f;
        [Min(1)] public int coinRingSlots = 6;
        [Tooltip("Tapping a coin also collects every coin within this distance.")]
        [Min(0f)] public float coinCollectRadius = 3f;
        [Tooltip("A managed stand's coin floats up for this long, then collects itself.")]
        [Min(0.05f)] public float managerCollectSeconds = 0.8f;
        [Min(0f)] public float managerCoinRise = 1.6f;

        [Header("Visitors")]
        [Min(0f)] public float visitorBobHeight = 0.05f;
        [Min(0f)] public float visitorBobSpeed = 9f;
        [Min(0f)] public float visitorTurnDegreesPerSecond = 540f;
        [Min(0f)] public float visitorLimbSwingDegrees = 32f;
        [Tooltip("Height of a lying visitor's body above the towel.")]
        [Min(0f)] public float visitorLyingHeight = 0.12f;

        [Header("Floating +N text")]
        [Min(1)] public int floatingTextPoolSize = 12;
        [Min(0.1f)] public float floatingTextSeconds = 0.9f;
        [Min(0f)] public float floatingTextRisePixels = 140f;

        [Tooltip("A new coin pops out of its stand in an arc over this many seconds.")]
        [Min(0.01f)] public float coinPopSeconds = 0.35f;
        [Min(0f)] public float coinPopHeight = 1.2f;

        [Header("Feel")]
        [Tooltip("Size of the bounce when a building finishes or a stand levels up (0.2 = 20% bigger).")]
        [Range(0f, 1f)] public float punchScale = 0.2f;
        [Min(0.05f)] public float punchSeconds = 0.4f;
        [Tooltip("Confetti pieces for a finished building, a passed mini-game level, a trophy, a new neighbour.")]
        [Min(0)] public int tierConfetti = 80;
        [Min(0)] public int levelConfetti = 50;
        [Min(0)] public int trophyConfetti = 35;
        [Min(0)] public int neighbourConfetti = 35;

        [Header("Construction")]
        [Tooltip("Scale of the construction crate at 0% progress (grows to 1).")]
        [Range(0.05f, 1f)] public float constructionStartScale = 0.3f;
    }
}
