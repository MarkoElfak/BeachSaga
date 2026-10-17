using BeachTycoon.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// Confetti over the whole screen for big moments (ConfettiRequested): pooled coloured pieces burst up from
    /// the bottom edge, flutter and fall away. Unscaled time, no allocations; switches off when the last lands.
    /// </summary>
    public sealed class ConfettiView : MonoBehaviour
    {
        [SerializeField] private RectTransform area = null;
        [SerializeField] private Image[] pieces = new Image[0];
        [SerializeField] private Color[] colors =
        {
            new Color(1f, 0.42f, 0.42f), new Color(1f, 0.82f, 0.3f), new Color(0.35f, 0.8f, 0.95f),
            new Color(0.55f, 0.88f, 0.5f), new Color(0.95f, 0.55f, 0.85f), Color.white,
        };
        [SerializeField, Min(0.1f)] private float lifetime = 2.6f;
        [Tooltip("Launch speed range, in screen heights per second.")]
        [SerializeField] private Vector2 launchSpeed = new Vector2(1.1f, 1.7f);
        [Tooltip("Pull downwards, in screen heights per second squared.")]
        [SerializeField, Min(0f)] private float gravity = 1.6f;

        private Vector2[] _velocity;
        private float[] _age;
        private float[] _spin;
        private int _active;

        public int ActivePieces => _active;

        /// <summary>Seconds of flight since the last burst.</summary>
        public float SinceBurst { get; private set; }

        private void Awake()
        {
            _velocity = new Vector2[pieces.Length];
            _age = new float[pieces.Length];
            _spin = new float[pieces.Length];
            for (int i = 0; i < pieces.Length; i++) pieces[i].gameObject.SetActive(false);
            EventBus.Subscribe<ConfettiRequested>(OnRequested);
            enabled = false;
        }

        private void OnDestroy() => EventBus.Unsubscribe<ConfettiRequested>(OnRequested);

        public void Burst(int count)
        {
            Vector2 size = area.rect.size;
            count = Mathf.Min(count, pieces.Length);
            for (; count > 0; count--)
            {
                int i = FreePiece();
                if (!pieces[i].gameObject.activeSelf) _active++;
                _age[i] = 0f;
                float side = Random.value < 0.5f ? -1f : 1f; // two fountains from the bottom corners
                float speed = Random.Range(launchSpeed.x, launchSpeed.y) * size.y;
                float angle = Random.Range(55f, 85f) * Mathf.Deg2Rad;
                _velocity[i] = new Vector2(-side * Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                _spin[i] = Random.Range(-540f, 540f);
                RectTransform rect = pieces[i].rectTransform;
                rect.anchoredPosition = new Vector2(side * size.x * Random.Range(0.35f, 0.5f), -size.y * 0.5f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                pieces[i].color = colors[Random.Range(0, colors.Length)];
                pieces[i].gameObject.SetActive(true);
            }

            SinceBurst = 0f;
            if (_active > 0) enabled = true;
        }

        // A resting piece, or else the oldest one in the air, so a burst right after another still shows in full.
        private int FreePiece()
        {
            int oldest = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!pieces[i].gameObject.activeSelf) return i;
                if (_age[i] > _age[oldest]) oldest = i;
            }

            return oldest;
        }

        private void OnRequested(ConfettiRequested evt) => Burst(evt.Pieces);

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float fall = gravity * area.rect.height;
            SinceBurst += dt;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!pieces[i].gameObject.activeSelf) continue;
                _age[i] += dt;
                if (_age[i] >= lifetime)
                {
                    pieces[i].gameObject.SetActive(false);
                    _active--;
                    continue;
                }

                // Air drag slows the burst; a sway makes pieces flutter instead of dropping straight down.
                _velocity[i] = _velocity[i] * (1f - 1.2f * dt) + new Vector2(Mathf.Sin(_age[i] * 7f + i) * 400f, -fall) * dt;
                RectTransform rect = pieces[i].rectTransform;
                rect.anchoredPosition += _velocity[i] * dt;
                rect.localRotation *= Quaternion.Euler(0f, 0f, _spin[i] * dt);
                rect.localScale = new Vector3(Mathf.Cos(_age[i] * 9f + i), 1f, 1f); // tumbling
            }

            if (_active <= 0) enabled = false;
        }
    }
}
