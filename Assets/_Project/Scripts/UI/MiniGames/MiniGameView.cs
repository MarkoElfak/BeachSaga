using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Screen of one mini-game inside the mini-game panel: owns the game model, turns taps into its inputs and
    /// draws it each frame (no allocations; texts only change on events).
    /// </summary>
    public abstract class MiniGameView : MonoBehaviour
    {
        public abstract MiniGameKind Kind { get; }
        public abstract MiniGame Game { get; }

        /// <summary>Called right after the game began: lay out for this level's choices.</summary>
        public virtual void OnBegin()
        {
        }

        public abstract void Render();

        protected static void SetActive(GameObject target, bool active)
        {
            if (target.activeSelf != active) target.SetActive(active);
        }

        /// <summary>Shows a 0..1 bar by scaling its fill (pivoted at its left edge) across.</summary>
        public static void SetFill(RectTransform fill, float fraction)
        {
            fill.localScale = new Vector3(Mathf.Clamp01(fraction), 1f, 1f);
        }
    }
}
