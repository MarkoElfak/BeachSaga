using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>The village name and its culture's flag in the HUD's top bar (a plain icon until a culture is picked).</summary>
    public sealed class VillageNameView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label = null;
        [SerializeField] private RawImage flag = null;
        [SerializeField] private GameObject icon = null;

        private CultureService _cultures;

        public string Text => label.text;
        public Texture Flag => flag.gameObject.activeSelf ? flag.texture : null;

        private void Start()
        {
            if (!ServiceLocator.TryGet(out VillageService village)) return;
            ServiceLocator.TryGet(out _cultures);
            label.text = village.Name;
            ShowFlag();
            EventBus.Subscribe<VillageRenamed>(OnRenamed);
            EventBus.Subscribe<CultureChosen>(OnCultureChosen);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<VillageRenamed>(OnRenamed);
            EventBus.Unsubscribe<CultureChosen>(OnCultureChosen);
        }

        private void OnRenamed(VillageRenamed evt) => label.text = evt.Name;

        private void OnCultureChosen(CultureChosen evt) => ShowFlag();

        private void ShowFlag()
        {
            CultureDefinition culture = _cultures?.Current;
            flag.texture = culture != null ? _cultures.Flag(culture) : null;
            flag.gameObject.SetActive(culture != null);
            icon.SetActive(culture == null);
        }
    }
}
