using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// The player's character card: portrait, flag, name, ability and where they work. Opens when the character is
    /// tapped on the beach, or from the HUD portrait button, which also flies the camera over to them.
    /// </summary>
    public sealed class CharacterCardView : MonoBehaviour
    {
        [SerializeField] private Button openButton = null;
        [SerializeField] private RawImage openPortrait = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private RawImage portrait = null;
        [SerializeField] private RawImage flag = null;
        [SerializeField] private TMP_Text nameLabel = null;
        [SerializeField] private TMP_Text taglineLabel = null;
        [SerializeField] private TMP_Text abilityLabel = null;
        [SerializeField] private TMP_Text workLabel = null;
        [SerializeField] private TMP_Text hintLabel = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private Button okButton = null;

        private CharacterService _characters;
        private CultureService _cultures;
        private StandService _stands;

        public bool IsOpen => root.activeSelf;
        public bool ShowsButton => openButton.gameObject.activeSelf;
        public string NameText => nameLabel.text;
        public string WorkText => workLabel.text;
        public Texture PortraitImage => portrait.texture;

        private void Start()
        {
            root.SetActive(false);
            openButton.gameObject.SetActive(false);
            if (!ServiceLocator.TryGet(out _characters)) return;
            _cultures = ServiceLocator.Get<CultureService>();
            _stands = ServiceLocator.Get<StandService>();
            openButton.onClick.AddListener(OnOpenButton);
            closeButton.onClick.AddListener(Close);
            okButton.onClick.AddListener(Close);
            EventBus.Subscribe<CharacterJoined>(OnJoined);
            EventBus.Subscribe<CharacterCardRequested>(OnRequested);
            EventBus.Subscribe<CharacterAssigned>(OnAssigned);
            EventBus.Subscribe<CharacterStartedWork>(OnStartedWork);
            RefreshButton();
        }

        private void OnDestroy()
        {
            if (_characters == null) return;
            openButton.onClick.RemoveListener(OnOpenButton);
            closeButton.onClick.RemoveListener(Close);
            okButton.onClick.RemoveListener(Close);
            EventBus.Unsubscribe<CharacterJoined>(OnJoined);
            EventBus.Unsubscribe<CharacterCardRequested>(OnRequested);
            EventBus.Unsubscribe<CharacterAssigned>(OnAssigned);
            EventBus.Unsubscribe<CharacterStartedWork>(OnStartedWork);
        }

        public void Open()
        {
            CharacterDefinition character = _characters.Character;
            if (character == null) return;
            portrait.texture = _characters.Portrait(character);
            flag.texture = _cultures.Flag(_cultures.Current);
            nameLabel.text = character.displayName;
            taglineLabel.text = character.tagline;
            abilityLabel.text = character.ability;
            hintLabel.text = $"To move {character.displayName}, tap a stand, then Work here.";
            RefreshWork();
            root.SetActive(true);
        }

        public void Close() => root.SetActive(false);

        private void OnOpenButton()
        {
            EventBus.Publish(new CameraFocusRequested(_characters.Agent.Position));
            Open();
        }

        private void OnJoined(CharacterJoined evt) => RefreshButton();
        private void OnRequested(CharacterCardRequested evt) => Open();
        private void OnAssigned(CharacterAssigned evt) => RefreshWork();
        private void OnStartedWork(CharacterStartedWork evt) => RefreshWork();

        private void RefreshButton()
        {
            bool joined = _characters.HasJoined;
            openButton.gameObject.SetActive(joined);
            if (joined) openPortrait.texture = _characters.Portrait(_characters.Character);
        }

        private void RefreshWork()
        {
            if (_characters.Character == null) return;
            int stand = _characters.StandIndex;
            if (stand < 0)
            {
                workLabel.text = "Waiting for the first stand to open";
                return;
            }

            string product = _stands.Get(stand).Product.displayName;
            workLabel.text = _characters.IsWorking ? $"Works at {product}" : $"Walking over to {product}";
        }
    }
}
