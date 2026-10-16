using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Pretend visitors for a visited island: they arrive, browse the stands, relax on a towel and leave. No
    /// orders or coins, only movement, using the island's own WorldNavigation. Agents are preallocated.
    /// </summary>
    public sealed class VisitCrowdSimulation
    {
        private const float StandChance = 0.55f;
        private const float BrowseSpread = 0.45f;
        private const float BrowseDepth = 1.8f;

        private readonly WorldNavigation _navigation;
        private readonly OnlineConfig _config;
        private readonly VisitorConfig _visitors;
        private readonly Vector3[] _standFronts;
        private readonly System.Random _random;
        private readonly VisitorAgent[] _agents;
        private float _spawnTimer;

        public VisitCrowdSimulation(WorldNavigation navigation, OnlineConfig config, VisitorConfig visitors, int capacity,
            Vector3[] standFronts, int seed)
        {
            _navigation = navigation;
            _config = config;
            _visitors = visitors;
            _standFronts = standFronts;
            _random = new System.Random(seed);
            _agents = new VisitorAgent[Mathf.Max(0, capacity)];
            for (int i = 0; i < _agents.Length; i++) _agents[i] = new VisitorAgent(i);
        }

        public int Capacity => _agents.Length;
        public int ActiveCount { get; private set; }

        public VisitorAgent Get(int index) => _agents[index];

        /// <summary>Runs the crowd for a while up front, so the island is already busy when it appears.</summary>
        public void Warm(float seconds, float step)
        {
            for (float t = 0f; t < seconds; t += step) Tick(step);
        }

        public void Tick(float deltaTime)
        {
            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0f)
            {
                _spawnTimer = _config.visitorSpawnSeconds;
                TrySpawn();
            }

            float step = _visitors.walkSpeed * deltaTime;
            for (int i = 0; i < _agents.Length; i++)
            {
                VisitorAgent agent = _agents[i];
                if (!agent.IsActive) continue;
                switch (agent.State)
                {
                    case VisitorState.Relaxing:
                    case VisitorState.Buying:
                        agent.Timer -= deltaTime;
                        if (agent.Timer <= 0f) Next(agent);
                        break;
                    default:
                        if (agent.Move(step, _visitors.arriveDistance)) Arrived(agent);
                        break;
                }
            }
        }

        private void TrySpawn()
        {
            if (_navigation.EntryCount == 0) return;
            for (int i = 0; i < _agents.Length; i++)
            {
                VisitorAgent agent = _agents[i];
                if (agent.IsActive) continue;
                agent.Reset();
                agent.EntryIndex = _random.Next(_navigation.EntryCount);
                agent.Position = _navigation.GetEntry(agent.EntryIndex);
                agent.MaxPurchases = _random.Next(_config.activitiesRange.x, _config.activitiesRange.y + 1);
                _navigation.BuildPath(agent.Position, _navigation.GetBeachAccess(agent.Position), agent.Path);
                agent.State = VisitorState.Arriving;
                ActiveCount++;
                return;
            }
        }

        private void Arrived(VisitorAgent agent)
        {
            switch (agent.State)
            {
                case VisitorState.Arriving:
                    Next(agent);
                    break;
                case VisitorState.WalkingToSpot:
                    Wait(agent, VisitorState.Relaxing);
                    break;
                case VisitorState.Queueing:
                    agent.Facing = Vector3.forward; // looking at the counter
                    Wait(agent, VisitorState.Buying);
                    break;
                case VisitorState.Leaving:
                    agent.Reset();
                    ActiveCount--;
                    break;
            }
        }

        private void Next(VisitorAgent agent)
        {
            if (agent.Purchases >= agent.MaxPurchases)
            {
                Leave(agent);
                return;
            }

            agent.Purchases++;
            bool browse = _standFronts.Length > 0 && _random.NextDouble() < StandChance;
            if (!browse && agent.SpotIndex < 0) agent.SpotIndex = _navigation.ReserveSpot(_random);
            if (!browse && agent.SpotIndex >= 0)
            {
                _navigation.BuildPath(agent.Position, _navigation.GetSpot(agent.SpotIndex), agent.Path);
                agent.State = VisitorState.WalkingToSpot;
            }
            else if (_standFronts.Length > 0)
            {
                Vector3 front = _standFronts[_random.Next(_standFronts.Length)];
                front += new Vector3(Spread(BrowseSpread), 0f, -(float)_random.NextDouble() * BrowseDepth);
                _navigation.BuildPath(agent.Position, front, agent.Path);
                agent.State = VisitorState.Queueing;
            }
            else
            {
                Leave(agent); // no stands and the beach is full
            }
        }

        private void Leave(VisitorAgent agent)
        {
            _navigation.ReleaseSpot(agent.SpotIndex);
            agent.SpotIndex = -1;
            Vector3 entry = _navigation.GetEntry(agent.EntryIndex);
            Vector3 access = _navigation.GetBeachAccess(entry);
            _navigation.BuildPath(agent.Position, access, agent.Path);
            _navigation.AppendPath(access, entry, agent.Path);
            agent.State = VisitorState.Leaving;
        }

        private void Wait(VisitorAgent agent, VisitorState state)
        {
            Vector2 range = _config.activitySecondsRange;
            agent.Timer = Mathf.Lerp(range.x, range.y, (float)_random.NextDouble());
            agent.State = state;
        }

        private float Spread(float half) => ((float)_random.NextDouble() * 2f - 1f) * half;
    }
}
