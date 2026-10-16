using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Spinning, bobbing coin. Tapping it collects it and nearby coins; merged income makes it pulse. A managed
    /// stand's coin floats up and collects itself.
    /// </summary>
    public sealed class CoinPickupView : MonoBehaviour, ITappable
    {
        private const float PulseSeconds = 0.25f;
        private const float PulseScale = 0.35f;

        [SerializeField] private Transform visual = null;

        private CoinSpawner _owner;
        private PresentationConfig _config;
        private IEconomyService _economy;
        private Vector3 _basePosition;
        private long _amount;
        private float _age;
        private float _pulse;
        private float _autoCollectIn;
        private bool _collectable;

        public int SourceId { get; private set; }

        /// <summary>True while it is floating away to collect itself (a manager's coin).</summary>
        public bool IsAutoCollecting => _autoCollectIn > 0f;

        internal void Initialize(CoinSpawner owner, PresentationConfig config, IEconomyService economy)
        {
            _owner = owner;
            _config = config;
            _economy = economy;
        }

        public void Show(int sourceId, Vector3 position, long amount)
        {
            SourceId = sourceId;
            _basePosition = position;
            _amount = amount;
            _age = 0f;
            _pulse = 0f;
            _autoCollectIn = 0f;
            _collectable = true;
            transform.position = position;
            visual.localScale = Vector3.one;
        }

        public void AddAmount(long amount)
        {
            _amount += amount;
            _pulse = PulseSeconds;
        }

        /// <summary>Floats up and collects itself after PresentationConfig.managerCollectSeconds.</summary>
        public void StartAutoCollect()
        {
            if (_collectable && !IsAutoCollecting) _autoCollectIn = _config.managerCollectSeconds;
        }

        public void OnTapped()
        {
            if (_collectable)
            {
                _owner.CollectAround(this);
            }
        }

        internal void Collect()
        {
            if (!_collectable)
            {
                return;
            }

            _collectable = false; // guards against a double release in the same frame
            _economy.Earn(_amount);
            EventBus.Publish(new CoinCollected(_amount, transform.position));
            _owner.Release(this);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _age += deltaTime;
            visual.localRotation = Quaternion.Euler(0f, _age * _config.coinSpinDegreesPerSecond, 0f);
            float bob = Mathf.Sin(_age * _config.coinBobSpeed) * _config.coinBobHeight;
            transform.position = _basePosition + new Vector3(0f, bob + Rise(deltaTime), 0f);
            if (!_collectable) return;

            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - deltaTime);
                visual.localScale = Vector3.one * (1f + PulseScale * Mathf.Sin(_pulse / PulseSeconds * Mathf.PI));
            }
        }

        // Height of a manager's coin floating away; collects it once the float is over.
        private float Rise(float deltaTime)
        {
            if (!IsAutoCollecting) return 0f;
            _autoCollectIn -= deltaTime;
            float t = 1f - Mathf.Clamp01(_autoCollectIn / _config.managerCollectSeconds);
            if (_autoCollectIn <= 0f) Collect();
            return t * t * _config.managerCoinRise;
        }
    }
}
