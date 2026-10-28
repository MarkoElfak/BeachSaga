using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Spawns pooled visitors (interval = base / (1 + popularity) / event rate, capped by the tier's
    /// maxVisitors; an event's burst may go past the cap), picks their type by spawnWeight and ticks their
    /// state machines. All agents are preallocated.
    /// </summary>
    public sealed class VisitorService : ITickable, IDisposable
    {
        private readonly GameDatabase _database;
        private readonly BuildingManager _buildings;
        private readonly PopularityService _popularity;
        private readonly BeachEventService _events;
        private readonly WorldNavigation _navigation;
        private readonly VisitorStateMachine _machine;
        private readonly System.Random _random;
        private readonly VisitorAgent[] _agents;
        private readonly Stack<int> _free;
        private readonly List<VisitorAgent> _active;
        private float _spawnTimer;

        public VisitorService(GameDatabase database, BuildingManager buildings, StandService stands,
            PopularityService popularity, WorldNavigation navigation, BeachEventService events = null,
            CultureAbilities abilities = null)
        {
            _database = database;
            _events = events;
            _buildings = buildings;
            _popularity = popularity;
            _navigation = navigation;
            VisitorConfig config = database.visitors;
            _random = new System.Random(config.randomSeed);
            _machine = new VisitorStateMachine(navigation, stands, config, _random, Despawn, events, abilities);

            _agents = new VisitorAgent[config.poolMaxSize];
            _free = new Stack<int>(config.poolMaxSize);
            _active = new List<VisitorAgent>(config.poolMaxSize);
            for (int i = _agents.Length - 1; i >= 0; i--)
            {
                _agents[i] = new VisitorAgent(i);
                _free.Push(i);
            }

            _spawnTimer = SpawnInterval;
            EventBus.Subscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Subscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Subscribe<BeachEventStarted>(OnBeachEventStarted);
        }

        public int ActiveCount => _active.Count;
        public IReadOnlyList<VisitorAgent> Active => _active;

        public VisitorAgent Get(int visitorId) => _agents[visitorId];

        /// <summary>Seconds between arrivals right now (popularity and any running event included).</summary>
        public float SpawnInterval
        {
            get
            {
                float rate = (1f + _popularity.Value) * (_events != null ? _events.SpawnRateMultiplier : 1f);
                return _database.economy.baseSpawnIntervalSeconds / rate;
            }
        }

        private int MaxVisitors
        {
            get
            {
                TierDefinition tier = _database.GetTier(_buildings.MainTier);
                return tier != null ? Mathf.Min(tier.maxVisitors, _agents.Length) : 0;
            }
        }

        public void Tick(float deltaTime)
        {
            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = SpawnInterval;
                if (_active.Count < MaxVisitors && _navigation.EntryCount > 0)
                {
                    TrySpawn();
                }
            }

            // Backwards, so a despawn (swap-remove) never skips an agent.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _machine.Tick(_active[i], deltaTime);
            }
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Unsubscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Unsubscribe<BeachEventStarted>(OnBeachEventStarted);
        }

        /// <summary>Spawns one visitor right away if the pool allows (stress tests); false when it cannot.</summary>
        public bool TrySpawn()
        {
            int typeIndex = PickTypeIndex(_buildings.MainTier);
            if (typeIndex < 0 || _free.Count == 0 || _navigation.EntryCount == 0)
            {
                return false;
            }

            VisitorAgent agent = _agents[_free.Pop()];
            agent.Reset();
            agent.TypeIndex = typeIndex;
            agent.Type = _database.visitorTypes[typeIndex];
            agent.EntryIndex = _random.Next(_navigation.EntryCount);
            Vector2Int purchases = _database.visitors.purchasesRange;
            agent.MaxPurchases = _random.Next(purchases.x, purchases.y + 1);
            _machine.Begin(agent);
            _active.Add(agent);
            EventBus.Publish(new VisitorSpawned(agent.Id));
            return true;
        }

        private void Despawn(VisitorAgent agent)
        {
            _machine.Release(agent);
            int index = _active.IndexOf(agent);
            int last = _active.Count - 1;
            _active[index] = _active[last];
            _active.RemoveAt(last);
            agent.Reset();
            _free.Push(agent.Id);
            EventBus.Publish(new VisitorDespawned(agent.Id));
        }

        // A moved stand (or the stand row sliding with a growing building) moves its queue: visitors re-path.
        private void OnObjectMoved(ObjectMoved evt)
        {
            if (evt.Kind == PlaceableKind.Decoration) return;
            for (int i = 0; i < _active.Count; i++) _active[i].QueueSlot = -1;
        }

        // A tourist bus: a crowd arrives at once, and the next regular arrival comes at the faster rate.
        private void OnBeachEventStarted(BeachEventStarted evt)
        {
            int burst = _events != null && _events.Active != null ? _events.Active.burstVisitors : 0;
            for (int i = 0; i < burst && TrySpawn(); i++) { }
            _spawnTimer = Mathf.Min(_spawnTimer, SpawnInterval);
        }

        private void OnSaleCompleted(SaleCompleted evt)
        {
            if (evt.VisitorId >= 0 && evt.VisitorId < _agents.Length && _agents[evt.VisitorId].IsActive)
            {
                _machine.OnPurchased(_agents[evt.VisitorId]);
            }
        }

        /// <summary>Weighted pick among types whose minTier is reached and that have a prefab.</summary>
        private int PickTypeIndex(int tier)
        {
            VisitorTypeDefinition[] types = _database.visitorTypes;
            if (types == null)
            {
                return -1;
            }

            float total = 0f;
            for (int i = 0; i < types.Length; i++)
            {
                if (IsEligible(types[i], tier)) total += types[i].spawnWeight;
            }

            float roll = (float)_random.NextDouble() * total;
            for (int i = 0; i < types.Length; i++)
            {
                if (!IsEligible(types[i], tier)) continue;
                roll -= types[i].spawnWeight;
                if (roll <= 0f) return i;
            }

            return -1;
        }

        private static bool IsEligible(VisitorTypeDefinition type, int tier) =>
            type != null && type.prefab != null && type.minTier <= tier && type.spawnWeight > 0f;
    }
}
