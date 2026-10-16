using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Banner while something is being moved, and a one-time tip right after the tutorial that buildings and
    /// decorations can be moved with a long press.
    /// </summary>
    public sealed class MoveHintView : MonoBehaviour
    {
        private const string MovingText = "Drag to a green spot and let go";
        private const string TipText = "Tip: tap a building to turn it, press and hold to move it";
        private const float TipSeconds = 5f;

        [SerializeField] private GameObject banner = null;
        [SerializeField] private TMP_Text label = null;

        private float _tipRemaining;
        private bool _moving;

        private void Start()
        {
            banner.SetActive(false);
            EventBus.Subscribe<MoveStarted>(OnMoveStarted);
            EventBus.Subscribe<MoveEnded>(OnMoveEnded);
            EventBus.Subscribe<TutorialStepChanged>(OnTutorialStep);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<MoveStarted>(OnMoveStarted);
            EventBus.Unsubscribe<MoveEnded>(OnMoveEnded);
            EventBus.Unsubscribe<TutorialStepChanged>(OnTutorialStep);
        }

        private void OnMoveStarted(MoveStarted evt)
        {
            _moving = true;
            _tipRemaining = 0f;
            label.text = MovingText;
            banner.SetActive(true);
        }

        private void OnMoveEnded(MoveEnded evt)
        {
            _moving = false;
            banner.SetActive(false);
        }

        private void OnTutorialStep(TutorialStepChanged evt)
        {
            if (ServiceLocator.TryGet(out TutorialService tutorial) && !tutorial.IsActive && !_moving)
            {
                _tipRemaining = TipSeconds;
                label.text = TipText;
                banner.SetActive(true);
            }
        }

        private void Update()
        {
            if (_tipRemaining <= 0f) return;
            _tipRemaining -= Time.unscaledDeltaTime;
            if (_tipRemaining <= 0f && !_moving) banner.SetActive(false);
        }
    }
}
