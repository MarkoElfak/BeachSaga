using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>World-space bar made of two flat boxes. The fill pivot sits at the bar's left edge.</summary>
    public sealed class ProgressBarView : MonoBehaviour
    {
        [SerializeField] private Transform fillPivot = null;

        private float _shownProgress = -1f;

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (Mathf.Approximately(progress, _shownProgress))
            {
                return;
            }

            _shownProgress = progress;
            fillPivot.localScale = new Vector3(progress, 1f, 1f);
        }
    }
}
