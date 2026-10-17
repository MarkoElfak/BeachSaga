using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>The village name in the HUD's top bar.</summary>
    public sealed class VillageNameView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label = null;

        public string Text => label.text;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out VillageService village)) return;
            label.text = village.Name;
            EventBus.Subscribe<VillageRenamed>(OnRenamed);
        }

        private void OnDestroy() => EventBus.Unsubscribe<VillageRenamed>(OnRenamed);

        private void OnRenamed(VillageRenamed evt) => label.text = evt.Name;
    }
}
