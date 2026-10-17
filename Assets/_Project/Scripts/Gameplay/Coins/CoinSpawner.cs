using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;
using UnityEngine.Pool;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Pooled coin pickups. Each source (a stand or a house) shows at most coinRingSlots coins on a small ring;
    /// further income merges into its oldest coin. Tapping a coin collects every coin within coinCollectRadius.
    /// Coins from a stand with a manager collect themselves.
    /// </summary>
    public sealed class CoinSpawner : MonoBehaviour
    {
        [SerializeField] private CoinPickupView coinPrefab = null;

        private readonly List<CoinPickupView> _active = new List<CoinPickupView>();
        private readonly Dictionary<int, int> _nextSlotBySource = new Dictionary<int, int>();
        private ObjectPool<CoinPickupView> _pool;
        private PresentationConfig _config;
        private IEconomyService _economy;

        public void Initialize(PresentationConfig config, IEconomyService economy)
        {
            _config = config;
            _economy = economy;
            _pool = new ObjectPool<CoinPickupView>(
                CreateCoin,
                coin => coin.gameObject.SetActive(true),
                coin => coin.gameObject.SetActive(false),
                coin => Destroy(coin.gameObject),
                collectionCheck: false,
                defaultCapacity: config.coinPoolPrewarm,
                maxSize: config.coinPoolMaxSize);
            Prewarm(config.coinPoolPrewarm);
            EventBus.Subscribe<LeavingHome>(OnLeavingHome);
        }

        /// <summary>Position of a coin still waiting to be collected (tutorial arrow, tests).</summary>
        public bool TryGetWaitingCoin(out Vector3 position)
        {
            bool any = _active.Count > 0;
            position = any ? _active[0].transform.position : default;
            return any;
        }

        public void Spawn(int sourceId, Vector3 anchorPosition, long amount, bool autoCollect = false)
        {
            CoinPickupView oldest = null;
            int count = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].SourceId != sourceId) continue;
                count++;
                if (oldest == null) oldest = _active[i];
            }

            if (count >= _config.coinRingSlots && oldest != null)
            {
                oldest.AddAmount(amount);
                if (autoCollect) oldest.StartAutoCollect();
                return;
            }

            _nextSlotBySource.TryGetValue(sourceId, out int slot);
            _nextSlotBySource[sourceId] = (slot + 1) % _config.coinRingSlots;
            float angle = slot * (2f * Mathf.PI / _config.coinRingSlots);
            var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _config.coinRingRadius;

            CoinPickupView coin = _pool.Get();
            coin.Show(sourceId, anchorPosition + offset, amount, anchorPosition);
            _active.Add(coin);
            if (autoCollect) coin.StartAutoCollect();
        }

        /// <summary>Sends every coin waiting at one source off to collect itself (a manager was just hired).</summary>
        public void AutoCollectSource(int sourceId)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].SourceId == sourceId) _active[i].StartAutoCollect();
            }
        }

        /// <summary>Coins lying on the beach waiting for a tap (managers' coins are not counted).</summary>
        public int CountWaiting(int sourceId)
        {
            int count = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].SourceId == sourceId && !_active[i].IsAutoCollecting) count++;
            }

            return count;
        }

        /// <summary>Collects the tapped coin and every other coin near it.</summary>
        internal void CollectAround(CoinPickupView tapped)
        {
            Vector3 centre = tapped.transform.position;
            float radiusSqr = _config.coinCollectRadius * _config.coinCollectRadius;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                CoinPickupView coin = _active[i];
                if (coin == tapped || (coin.transform.position - centre).sqrMagnitude <= radiusSqr)
                {
                    coin.Collect();
                }
            }
        }

        internal void Release(CoinPickupView coin)
        {
            int last = _active.Count - 1;
            int index = _active.IndexOf(coin);
            if (index < 0) return;
            _active[index] = _active[last];
            _active.RemoveAt(last);
            _pool.Release(coin);
        }

        private CoinPickupView CreateCoin()
        {
            CoinPickupView coin = Instantiate(coinPrefab, transform, false);
            coin.Initialize(this, _config, _economy);
            coin.gameObject.SetActive(false);
            return coin;
        }

        private void Prewarm(int count)
        {
            var warm = new CoinPickupView[count];
            for (int i = 0; i < count; i++) warm[i] = _pool.Get();
            for (int i = 0; i < count; i++) _pool.Release(warm[i]);
        }

        // Leaving for a visit: coins still lying on the beach are collected rather than lost with the scene.
        private void OnLeavingHome(LeavingHome evt)
        {
            for (int i = _active.Count - 1; i >= 0; i--) _active[i].Collect();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LeavingHome>(OnLeavingHome);
            _pool?.Dispose();
        }
    }
}
