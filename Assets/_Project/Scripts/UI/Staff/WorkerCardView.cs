using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using BeachTycoon.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// A worker's or job seeker's card: portrait, country, level, wage and stars in every skill. One button per stand
    /// hires a seeker there, or moves a worker; workers can also be let go (tap twice). Opens when they are tapped.
    /// </summary>
    public sealed partial class WorkerCardView : MonoBehaviour
    {
        private static readonly Color StarOn = new Color(1f, 0.75f, 0.15f);
        private static readonly Color StarOff = new Color(0.82f, 0.8f, 0.76f);

        [SerializeField] private GameObject root = null;
        [SerializeField] private TMP_Text titleLabel = null;
        [SerializeField] private RawImage portrait = null;
        [SerializeField] private RawImage flag = null;
        [SerializeField] private TMP_Text nameLabel = null;
        [SerializeField] private TMP_Text infoLabel = null;
        [SerializeField] private TMP_Text statusLabel = null;
        [SerializeField] private TMP_Text[] skillLabels = new TMP_Text[0];
        [SerializeField] private Image[] stars = new Image[0];
        [SerializeField] private TMP_Text hintLabel = null;
        [SerializeField] private Button[] standButtons = new Button[0];
        [SerializeField] private Button dismissButton = null;
        [SerializeField] private TMP_Text dismissLabel = null;
        [SerializeField] private Button closeButton = null;

        private StaffService _staff;
        private StandService _stands;
        private CultureService _cultures;
        private CharacterService _characters;
        private StaffMember _member;
        private bool _confirmDismiss;

        public bool IsOpen => root.activeSelf;
        public int WorkerId => _member != null ? _member.Id : -1;
        public string NameText => nameLabel.text;
        public string StatusText => statusLabel.text;
        public string HintText => hintLabel.text;
        public Button StandButton(int index) => standButtons[index];
        public Button DismissButton => dismissButton;

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _staff)) return;
            _stands = ServiceLocator.Get<StandService>();
            _cultures = ServiceLocator.Get<CultureService>();
            _characters = ServiceLocator.Get<CharacterService>();
            for (int i = 0; i < standButtons.Length; i++)
            {
                int stand = i;
                standButtons[i].onClick.AddListener(() => OnStand(stand));
            }

            dismissButton.onClick.AddListener(OnDismiss);
            closeButton.onClick.AddListener(Close);
            Subscribe(true);
        }

        private void OnDestroy()
        {
            if (_staff == null) return;
            for (int i = 0; i < standButtons.Length; i++) standButtons[i].onClick.RemoveAllListeners();
            dismissButton.onClick.RemoveListener(OnDismiss);
            closeButton.onClick.RemoveListener(Close);
            Subscribe(false);
        }

        public void Open(int workerId)
        {
            _member = _staff.Find(workerId);
            if (_member == null) return;
            WorkerModel model = _member.Model;
            portrait.texture = _staff.Portraits.Get(model);
            CultureDefinition culture = _cultures.Find(model.cultureId);
            flag.texture = culture != null ? _cultures.Flag(culture) : null;
            flag.enabled = culture != null;
            nameLabel.text = model.name;
            _confirmDismiss = false;
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            root.SetActive(false);
            _member = null;
        }

        private void OnStand(int stand)
        {
            if (_member == null) return;
            bool done = _member.IsSeeker ? _staff.TryHire(_member.Id, stand) : _staff.TryMove(_member.Id, stand);
            if (done) Close();
        }

        // The first tap asks; the second lets them go.
        private void OnDismiss()
        {
            if (_member == null || _member.IsSeeker) return;
            if (!_confirmDismiss)
            {
                _confirmDismiss = true;
                dismissLabel.text = $"Tap again to let {_member.Model.name} go";
                return;
            }

            _staff.TryDismiss(_member.Id);
            Close();
        }

        private void Refresh()
        {
            if (_member == null) return;
            WorkerModel model = _member.Model;
            bool seeker = _member.IsSeeker;
            titleLabel.text = seeker ? "Job seeker" : "Your staff";
            CultureDefinition culture = _cultures.Find(model.cultureId);
            string country = culture != null ? culture.displayName : "Abroad";
            infoLabel.text = $"From {country}{(_staff.IsFromHome(model) ? " (home bonus)" : string.Empty)}\nLevel {model.level}\n"
                + $"Wage: {NumberFormatter.Format(_staff.Wage(model))} {PayPeriod()}";
            statusLabel.text = Status(model, seeker);
            RefreshSkills(model);
            RefreshStands(model, seeker);
            dismissButton.gameObject.SetActive(!seeker);
            if (!_confirmDismiss) dismissLabel.text = $"Let {model.name} go";
        }

        private void RefreshSkills(WorkerModel model)
        {
            int perRow = stars.Length / Mathf.Max(1, skillLabels.Length);
            for (int row = 0; row < skillLabels.Length; row++)
            {
                var skill = (StaffSkill)row;
                skillLabels[row].text = StaffSkills.Name(skill);
                int count = model.Stars(skill);
                for (int i = 0; i < perRow; i++) stars[row * perRow + i].color = i < count ? StarOn : StarOff;
            }
        }
    }
}
