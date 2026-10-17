using System.Collections.Generic;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Shows VisitCrowdSimulation's visitors: one preallocated view per agent, switched on and off.</summary>
    public sealed class VisitCrowd : MonoBehaviour
    {
        private const float WarmSeconds = 25f;
        private const float WarmStep = 0.1f;

        private VisitCrowdSimulation _simulation;
        private VisitorView[] _views = new VisitorView[0];
        private PresentationConfig _presentation;
        private TerrainShape _ground;

        public int ActiveCount => _simulation != null ? _simulation.ActiveCount : 0;

        public void Initialize(GameDatabase database, int tier, WorldNavigation navigation, IslandLayout layout, TerrainShape ground)
        {
            _presentation = database.presentation;
            _ground = ground;
            var types = new List<VisitorView>();
            var weights = new List<float>();
            foreach (VisitorTypeDefinition type in database.visitorTypes ?? new VisitorTypeDefinition[0])
            {
                VisitorView prefab = type != null && type.prefab != null ? type.prefab.GetComponent<VisitorView>() : null;
                if (prefab == null || type.minTier > tier || type.spawnWeight <= 0f) continue;
                types.Add(prefab);
                weights.Add(type.spawnWeight);
            }

            TierDefinition tierDefinition = database.GetTier(tier);
            int capacity = tierDefinition != null && types.Count > 0
                ? Mathf.Min(database.online.maxVisitors, tierDefinition.maxVisitors, database.visitors.poolMaxSize)
                : 0;
            _simulation = new VisitCrowdSimulation(navigation, database.online, database.visitors, capacity,
                StandFronts(database, layout), database.visitors.randomSeed);

            var random = new System.Random(database.visitors.randomSeed + 1);
            _views = new VisitorView[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _views[i] = Instantiate(types[Pick(weights, random)], transform, false);
                GpuInstancing.Enable(_views[i].gameObject);
                _views[i].gameObject.SetActive(false);
            }

            _simulation.Warm(WarmSeconds, WarmStep);
            Sync();
        }

        private void Update()
        {
            if (_simulation == null) return;
            _simulation.Tick(Time.deltaTime);
            Sync();
        }

        private void Sync()
        {
            for (int i = 0; i < _views.Length; i++)
            {
                VisitorAgent agent = _simulation.Get(i);
                if (_views[i].gameObject.activeSelf == agent.IsActive) continue;
                if (agent.IsActive) _views[i].Bind(agent, _presentation, _ground);
                _views[i].gameObject.SetActive(agent.IsActive);
            }
        }

        // Where a visitor stands to browse a stand: the start of its queue.
        private static Vector3[] StandFronts(GameDatabase database, IslandLayout layout)
        {
            var fronts = new Vector3[layout.Stands.Count];
            for (int i = 0; i < fronts.Length; i++)
            {
                ProductDefinition product = database.FindProduct(layout.Stands[i].productId);
                Vector2 at = layout.Placement.GetStandPosition(i) + (product != null ? product.queueStart : Vector2.zero);
                fronts[i] = new Vector3(at.x, 0f, at.y);
            }

            return fronts;
        }

        private static int Pick(List<float> weights, System.Random random)
        {
            float total = 0f;
            for (int i = 0; i < weights.Count; i++) total += weights[i];
            float roll = (float)random.NextDouble() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0f) return i;
            }

            return weights.Count - 1;
        }
    }
}
