using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// A quick springy bounce of its own transform's scale (a building finishing, a stand levelling up, the coin
    /// counter). Runs on unscaled time and switches itself off when done, so it costs nothing while idle.
    /// </summary>
    public sealed class ScalePunch : MonoBehaviour
    {
        private Vector3 _restScale = Vector3.one;
        private float _amount;
        private float _seconds = 0.4f;
        private float _age;

        public bool IsPlaying => enabled;

        /// <summary>Adds the component to target when missing and bounces it by amount (0.2 = 20% bigger).</summary>
        public static ScalePunch On(Component target, float amount, float seconds)
        {
            if (!target.TryGetComponent(out ScalePunch punch))
            {
                punch = target.gameObject.AddComponent<ScalePunch>();
                punch._restScale = target.transform.localScale;
            }

            punch.Play(amount, seconds);
            return punch;
        }

        public void Play(float amount, float seconds)
        {
            _amount = amount;
            _seconds = Mathf.Max(0.05f, seconds);
            _age = 0f;
            enabled = true;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = _age / _seconds;
            if (t >= 1f)
            {
                transform.localScale = _restScale;
                enabled = false;
                return;
            }

            // A decaying wobble: bigger first, a little smaller, then settled.
            float wobble = Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t);
            transform.localScale = _restScale * (1f + _amount * wobble);
        }
    }
}
