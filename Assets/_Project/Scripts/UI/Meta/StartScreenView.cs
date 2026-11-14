using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// The start screen over the beach. A new game asks for the village name and its character (who comes with a
    /// culture); a village from before cultures picks only the character; later launches greet the player with the
    /// flag, the village name and a Play button, once per app start. Settings reopens the name page to rename the village.
    /// </summary>
    public sealed class StartScreenView : MonoBehaviour
    {
        private enum Mode { Found, Rename, Culture }

        [SerializeField] private GameObject root = null;
        [SerializeField] private GameObject titlePage = null;
        [SerializeField] private RawImage titleFlag = null;
        [SerializeField] private TMP_Text villageLabel = null;
        [SerializeField] private TMP_Text cultureLabel = null;
        [SerializeField] private Button playButton = null;
        [SerializeField] private GameObject namePage = null;
        [SerializeField] private TMP_Text heading = null;
        [SerializeField] private TMP_Text hint = null;
        [SerializeField] private TMP_InputField nameInput = null;
        [SerializeField] private Button ideaButton = null;
        [SerializeField] private CulturePickerView picker = null;
        [Tooltip("How far the culture cards move up when the name field is hidden.")]
        [SerializeField] private float cultureOnlyLift = 190f;
        [SerializeField] private TMP_Text error = null;
        [SerializeField] private Button confirmButton = null;
        [SerializeField] private TMP_Text confirmLabel = null;
        [SerializeField] private Button closeButton = null;

        private static bool _greetedThisRun;
        private VillageService _village;
        private CultureService _cultures;
        private Mode _mode;
        private Vector2 _pickerHome;

        public bool IsOpen => root.activeSelf;
        public bool IsTitlePage => IsOpen && titlePage.activeSelf;
        public bool IsNamePage => IsOpen && namePage.activeSelf;
        public bool ShowsNameField => nameInput.gameObject.activeSelf;
        public bool ShowsCultures => picker.gameObject.activeSelf;
        public bool CanClose => closeButton.gameObject.activeSelf;
        public string VillageText => villageLabel.text;
        public string CultureText => cultureLabel.text;
        public Texture TitleFlag => titleFlag.texture;
        public string ErrorText => error.text;
        public string HeadingText => heading.text;
        public CulturePickerView Picker => picker;

        public string InputText
        {
            get => nameInput.text;
            set => nameInput.text = value;
        }

        /// <summary>Playtests that are not about the start screen skip the greeting.</summary>
        public static void SkipGreetingThisRun() => _greetedThisRun = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayModeEnter() => _greetedThisRun = false;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _village) || !ServiceLocator.TryGet(out _cultures)) return;

            nameInput.characterLimit = _village.MaxNameLength + 10; // room for spaces that are trimmed away
            _pickerHome = ((RectTransform)picker.transform).anchoredPosition;
            ServiceLocator.TryGet(out CharacterService characters);
            picker.Build(_cultures, characters);
            picker.Picked += OnPicked;
            playButton.onClick.AddListener(Close);
            ideaButton.onClick.AddListener(SuggestName);
            confirmButton.onClick.AddListener(Confirm);
            closeButton.onClick.AddListener(Close);
            nameInput.onValueChanged.AddListener(ClearError);
            nameInput.onSubmit.AddListener(Submit);

            if (!_village.IsFounded) OpenNamePage(Mode.Found);
            else if (_village.NeedsCulture) OpenNamePage(Mode.Culture);
            else if (!_greetedThisRun) OpenTitlePage();
            _greetedThisRun = true;
        }

        private void OnDestroy()
        {
            picker.Picked -= OnPicked;
            playButton.onClick.RemoveListener(Close);
            ideaButton.onClick.RemoveListener(SuggestName);
            confirmButton.onClick.RemoveListener(Confirm);
            closeButton.onClick.RemoveListener(Close);
            nameInput.onValueChanged.RemoveListener(ClearError);
            nameInput.onSubmit.RemoveListener(Submit);
        }

        /// <summary>From Settings: the name page with the current name and a close button.</summary>
        public void OpenRename() => OpenNamePage(Mode.Rename);

        public void Close() => root.SetActive(false);

        private void OpenTitlePage()
        {
            CultureDefinition culture = _cultures.Current;
            villageLabel.text = _village.Name;
            cultureLabel.text = culture != null ? culture.displayName : string.Empty;
            titleFlag.texture = _cultures.Flag(culture);
            titleFlag.transform.parent.gameObject.SetActive(culture != null); // the flag with its frame
            titlePage.SetActive(true);
            namePage.SetActive(false);
            root.SetActive(true);
        }

        private void OpenNamePage(Mode mode)
        {
            _mode = mode;
            heading.text = mode == Mode.Rename ? "Rename your village" : mode == Mode.Culture ? "Choose your character" : "Found your village";
            hint.text = mode == Mode.Culture ? "Pick once: it is for the whole game." : "Rename it any time. Your character is for good.";
            hint.gameObject.SetActive(mode != Mode.Rename);
            confirmLabel.text = mode == Mode.Rename ? "Save" : mode == Mode.Culture ? "Confirm" : "Found village";
            closeButton.gameObject.SetActive(mode == Mode.Rename);
            nameInput.gameObject.SetActive(mode != Mode.Culture);
            ideaButton.gameObject.SetActive(mode != Mode.Culture);
            nameInput.SetTextWithoutNotify(mode == Mode.Rename ? _village.Name : string.Empty);
            picker.gameObject.SetActive(mode != Mode.Rename);
            picker.ShowLabel(mode != Mode.Culture);
            ((RectTransform)picker.transform).anchoredPosition = _pickerHome + (mode == Mode.Culture ? Vector2.up * cultureOnlyLift : Vector2.zero);
            picker.Select(string.Empty);
            error.text = string.Empty;
            titlePage.SetActive(false);
            namePage.SetActive(true);
            root.SetActive(true);
        }

        private void SuggestName()
        {
            CultureDefinition culture = _cultures.Find(picker.PickedId);
            if (culture == null) culture = _cultures.Current;
            nameInput.text = _village.SuggestName(culture != null ? culture.villageNames : null);
        }

        private void OnPicked(string cultureId) => error.text = string.Empty;

        private void Submit(string _) => Confirm();

        private void Confirm()
        {
            string problem;
            bool done = _mode == Mode.Rename ? _village.TryRename(nameInput.text, out problem)
                : _mode == Mode.Culture ? _village.TryChooseCulture(picker.PickedId, out problem)
                : _village.TryFound(nameInput.text, picker.PickedId, out problem);
            if (done) Close();
            else error.text = problem;
        }

        private void ClearError(string _)
        {
            if (error.text.Length > 0) error.text = string.Empty;
        }
    }
}
