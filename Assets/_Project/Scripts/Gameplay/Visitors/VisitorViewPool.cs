using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace BeachTycoon.Gameplay
{
    /// <summary>One object pool per visitor type prefab; shows a view for every active VisitorAgent.</summary>
    public sealed class VisitorViewPool : MonoBehaviour
    {
        private readonly List<ObjectPool<VisitorView>> _pools = new List<ObjectPool<VisitorView>>();
        private VisitorView[] _viewsById;
        private int[] _poolIndexById;
        private VisitorService _visitors;
        private PresentationConfig _presentation;
        private TerrainShape _ground;

        public void Initialize(GameDatabase database, VisitorService visitors, TerrainShape ground)
        {
            _visitors = visitors;
            _ground = ground;
            _presentation = database.presentation;
            int capacity = database.visitors.poolMaxSize;
            _viewsById = new VisitorView[capacity];
            _poolIndexById = new int[capacity];

            VisitorTypeDefinition[] types = database.visitorTypes ?? new VisitorTypeDefinition[0];
            for (int i = 0; i < types.Length; i++)
            {
                _pools.Add(CreatePool(types[i], database.visitors.poolPrewarm, capacity));
            }

            EventBus.Subscribe<VisitorSpawned>(OnSpawned);
            EventBus.Subscribe<VisitorDespawned>(OnDespawned);

            IReadOnlyList<VisitorAgent> active = visitors.Active;
            for (int i = 0; i < active.Count; i++)
            {
                Show(active[i].Id);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<VisitorSpawned>(OnSpawned);
            EventBus.Unsubscribe<VisitorDespawned>(OnDespawned);
        }

        private void OnSpawned(VisitorSpawned evt) => Show(evt.VisitorId);

        private void OnDespawned(VisitorDespawned evt)
        {
            VisitorView view = _viewsById[evt.VisitorId];
            if (view == null)
            {
                return;
            }

            view.Unbind();
            _pools[_poolIndexById[evt.VisitorId]].Release(view);
            _viewsById[evt.VisitorId] = null;
        }

        private void Show(int visitorId)
        {
            VisitorAgent agent = _visitors.Get(visitorId);
            ObjectPool<VisitorView> pool = agent.TypeIndex < _pools.Count ? _pools[agent.TypeIndex] : null;
            if (pool == null || _viewsById[visitorId] != null)
            {
                return;
            }

            VisitorView view = pool.Get();
            view.Bind(agent, _presentation, _ground);
            _viewsById[visitorId] = view;
            _poolIndexById[visitorId] = agent.TypeIndex;
        }

        private ObjectPool<VisitorView> CreatePool(VisitorTypeDefinition type, int prewarm, int maxSize)
        {
            VisitorView prefab = type != null && type.prefab != null ? type.prefab.GetComponent<VisitorView>() : null;
            if (prefab == null)
            {
                return null; // VisitorService never spawns a type without a prefab
            }

            var pool = new ObjectPool<VisitorView>(
                () => Instantiate(prefab, transform, false),
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => Destroy(view.gameObject),
                false, prewarm, maxSize);

            var warm = new VisitorView[prewarm];
            for (int i = 0; i < prewarm; i++) warm[i] = pool.Get();
            for (int i = 0; i < prewarm; i++) pool.Release(warm[i]);
            return pool;
        }
    }
}
