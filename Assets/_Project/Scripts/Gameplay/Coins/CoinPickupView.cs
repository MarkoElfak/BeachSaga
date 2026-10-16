using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Spinning, bobbing coin. Tapping it collects it and nearby coins; merged income makes it pulse.</summary>
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
        private bool _collectable;

        public int SourceId { get; private set; }

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
            _collectable = true;
            transform.position = position;
            visual.localScale = Vector3.one;
        }

        public void AddAmount(long amount)
        {
            _amount += amount;
            _pulse = PulseSeconds;
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
            transform.position = _basePosition + new Vector3(0f, bob, 0f);

            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - deltaTime);
                visual.localScale = Vector3.one * (1f + PulseScale * Mathf.Sin(_pulse / PulseSeconds * Mathf.PI));
            }
        }
    }
}
