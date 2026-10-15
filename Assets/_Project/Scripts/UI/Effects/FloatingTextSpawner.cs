using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;
using UnityEngine.Pool;

namespace BeachTycoon.UI
{
    /// <summary>Shows a pooled "+N" label where a coin was collected.</summary>
    public sealed class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private FloatingTextItem itemPrefab = null;
        [SerializeField] private RectTransform layer = null;
        [SerializeField] private Camera worldCamera = null;

        private ObjectPool<FloatingTextItem> _pool;
        private PresentationConfig _config;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out GameDatabase database))
            {
                return;
            }

            _config = database.presentation;
            int size = _config.floatingTextPoolSize;
            _pool = new ObjectPool<FloatingTextItem>(
                () => Instantiate(itemPrefab, layer, false),
                item => item.gameObject.SetActive(true),
                item => item.gameObject.SetActive(false),
                item => Destroy(item.gameObject),
                false, size, size * 4);

            var warm = new FloatingTextItem[size];
            for (int i = 0; i < size; i++) warm[i] = _pool.Get();
            for (int i = 0; i < size; i++) _pool.Release(warm[i]);

            EventBus.Subscribe<CoinCollected>(OnCoinCollected);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CoinCollected>(OnCoinCollected);
            _pool?.Dispose();
        }

        internal void Release(FloatingTextItem item) => _pool.Release(item);

        private void OnCoinCollected(CoinCollected evt)
        {
            Vector3 screen = worldCamera.WorldToScreenPoint(evt.WorldPosition);
            if (screen.z < 0f ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local))
            {
                return;
            }

            _pool.Get().Play(this, local, evt.Amount, _config.floatingTextSeconds, _config.floatingTextRisePixels);
        }
    }
}
