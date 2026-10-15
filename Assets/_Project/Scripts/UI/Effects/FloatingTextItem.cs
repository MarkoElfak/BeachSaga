using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>One pooled "+N" label: rises and fades, then returns itself to the spawner.</summary>
    public sealed class FloatingTextItem : MonoBehaviour
    {
        private const string Format = "+{0:0}";

        [SerializeField] private TMP_Text label = null;

        private RectTransform _rect;
        private FloatingTextSpawner _owner;
        private Vector2 _start;
        private float _age;
        private float _duration;
        private float _rise;

        public void Play(FloatingTextSpawner owner, Vector2 anchoredPosition, long amount, float duration, float rise)
        {
            _rect ??= (RectTransform)transform;
            _owner = owner;
            _start = anchoredPosition;
            _duration = duration;
            _rise = rise;
            _age = 0f;
            _rect.anchoredPosition = anchoredPosition;
            label.alpha = 1f;
            label.SetText(Format, (float)amount);
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_age / _duration);
            _rect.anchoredPosition = _start + new Vector2(0f, _rise * t);
            label.alpha = 1f - t * t;
            if (t >= 1f)
            {
                _owner.Release(this);
            }
        }
    }
}
