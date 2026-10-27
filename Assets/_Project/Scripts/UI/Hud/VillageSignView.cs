using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>The wooden welcome sign where visitors walk in, showing the village name.</summary>
    public sealed class VillageSignView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label = null;

        private bool _placed;

        public string Text => label.text;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out VillageService village))
            {
                enabled = false;
                return;
            }

            label.text = village.Name;
            EventBus.Subscribe<VillageRenamed>(OnRenamed);
        }

        private void OnDestroy() => EventBus.Unsubscribe<VillageRenamed>(OnRenamed);

        // The ground is built at runtime, so the sign settles onto it once it exists.
        private void LateUpdate()
        {
            if (_placed || !ServiceLocator.TryGet(out TerrainShape ground)) return;
            VillageConfig config = ServiceLocator.Get<GameDatabase>().village;
            transform.SetPositionAndRotation(ground.Ground(config.signPosition.x, config.signPosition.y),
                Quaternion.Euler(0f, config.signYaw, 0f));
            _placed = true;
            enabled = false;
        }

        private void OnRenamed(VillageRenamed evt) => label.text = evt.Name;
    }
}
