using BeachTycoon.Core;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// The start screen over the beach. A new game (or a save from before villages) asks for the village name;
    /// later launches greet the player with the village name and a Play button, once per app start. Settings
    /// reopens the name page to rename the village.
    /// </summary>
    public sealed class StartScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject root = null;
        [SerializeField] private GameObject titlePage = null;
        [SerializeField] private TMP_Text villageLabel = null;
        [SerializeField] private Button playButton = null;
        [SerializeField] private GameObject namePage = null;
        [SerializeField] private TMP_Text heading = null;
        [SerializeField] private TMP_InputField nameInput = null;
        [SerializeField] private Button ideaButton = null;
        [SerializeField] private TMP_Text error = null;
        [SerializeField] private Button confirmButton = null;
        [SerializeField] private TMP_Text confirmLabel = null;
        [SerializeField] private Button closeButton = null;

        private static bool _greetedThisRun;
        private VillageService _village;
        private bool _renaming;

        public bool IsOpen => root.activeSelf;
        public bool IsTitlePage => IsOpen && titlePage.activeSelf;
        public bool IsNamePage => IsOpen && namePage.activeSelf;
        public bool CanClose => closeButton.gameObject.activeSelf;
        public string VillageText => villageLabel.text;
        public string ErrorText => error.text;
        public string HeadingText => heading.text;

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
            if (!ServiceLocator.TryGet(out _village)) return;

            nameInput.characterLimit = _village.MaxNameLength + 10; // room for spaces that are trimmed away
            playButton.onClick.AddListener(Close);
            ideaButton.onClick.AddListener(SuggestName);
            confirmButton.onClick.AddListener(Confirm);
            closeButton.onClick.AddListener(Close);
            nameInput.onValueChanged.AddListener(ClearError);
            nameInput.onSubmit.AddListener(Submit);

            if (!_village.IsFounded) OpenNamePage(false);
            else if (!_greetedThisRun) OpenTitlePage();
            _greetedThisRun = true;
        }

        private void OnDestroy()
        {
            playButton.onClick.RemoveListener(Close);
            ideaButton.onClick.RemoveListener(SuggestName);
            confirmButton.onClick.RemoveListener(Confirm);
            closeButton.onClick.RemoveListener(Close);
            nameInput.onValueChanged.RemoveListener(ClearError);
            nameInput.onSubmit.RemoveListener(Submit);
        }

        /// <summary>From Settings: the name page with the current name and a close button.</summary>
        public void OpenRename() => OpenNamePage(true);

        public void Close() => root.SetActive(false);

        private void OpenTitlePage()
        {
            villageLabel.text = _village.Name;
            titlePage.SetActive(true);
            namePage.SetActive(false);
            root.SetActive(true);
        }

        private void OpenNamePage(bool renaming)
        {
            _renaming = renaming;
            heading.text = renaming ? "Rename your village" : "Name your village";
            confirmLabel.text = renaming ? "Save" : "Found village";
            closeButton.gameObject.SetActive(renaming);
            nameInput.SetTextWithoutNotify(renaming ? _village.Name : string.Empty);
            error.text = string.Empty;
            titlePage.SetActive(false);
            namePage.SetActive(true);
            root.SetActive(true);
        }

        private void SuggestName() => nameInput.text = _village.SuggestName();

        private void Submit(string _) => Confirm();

        // The culture is picked here too from Milestone 14; until then villages are founded without one.
        private void Confirm()
        {
            bool done = _renaming
                ? _village.TryRename(nameInput.text, out string problem)
                : _village.TryFound(nameInput.text, string.Empty, out problem);
            if (done) Close();
            else error.text = problem;
        }

        private void ClearError(string _)
        {
            if (error.text.Length > 0) error.text = string.Empty;
        }
    }
}
