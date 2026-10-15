using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>Visual-only tuning: frame rate, pools and small animations.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Presentation", fileName = "PresentationConfig")]
    public sealed class PresentationConfig : ScriptableObject
    {
        [Min(15)] public int targetFrameRate = 60;

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

        [Header("Construction")]
        [Tooltip("Scale of the construction crate at 0% progress (grows to 1).")]
        [Range(0.05f, 1f)] public float constructionStartScale = 0.3f;
    }
}
