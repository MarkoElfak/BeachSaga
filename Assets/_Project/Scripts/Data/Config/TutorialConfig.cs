using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>What the arrow points at during a tutorial step.</summary>
    public enum TutorialTarget
    {
        MainLot = 0,
        FirstStandOrCoin = 1,
        FirstProductButton = 2,
    }

    [Serializable]
    public struct TutorialStepDefinition
    {
        public string id;
        [TextArea] public string message;
        public TutorialTarget target;
    }

    /// <summary>The three-step first-session tutorial: build the tent, sell a donut, upgrade.</summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Tutorial", fileName = "TutorialConfig")]
    public sealed class TutorialConfig : ScriptableObject
    {
        public TutorialStepDefinition[] steps =
        {
            new TutorialStepDefinition { id = "build_tent", message = "Tap the empty lot to build your first tent", target = TutorialTarget.MainLot },
            new TutorialStepDefinition { id = "sell_donut", message = "Visitors buy donuts. Tap the coin to collect it!", target = TutorialTarget.FirstStandOrCoin },
            new TutorialStepDefinition { id = "upgrade", message = "Upgrade your donut stand to earn more per sale", target = TutorialTarget.FirstProductButton },
        };

        [Tooltip("World-space height above the target where the arrow hovers.")]
        [Min(0f)] public float worldArrowHeight = 3.2f;
        [Min(0f)] public float arrowBobPixels = 18f;
        [Min(0f)] public float arrowBobSpeed = 4f;
    }
}
