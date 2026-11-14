using System.Text;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeachTycoon.UI
{
    /// <summary>
    /// The staff list: everyone hired with their level, stand and wage, a Move button (opens their card) and a
    /// Dismiss button (tap twice), the next payday and the job seekers waiting. Its HUD button shows used slots and a
    /// badge while a seeker could be hired.
    /// </summary>
    public sealed class StaffPanelView : MonoBehaviour
    {
        [SerializeField] private Button openButton = null;
        [SerializeField] private TMP_Text openLabel = null;
        [SerializeField] private GameObject badge = null;
        [SerializeField] private GameObject root = null;
        [SerializeField] private Button closeButton = null;
        [SerializeField] private TMP_Text summaryLabel = null;
        [SerializeField] private TMP_Text seekersLabel = null;
        [SerializeField] private Button findButton = null;
        [SerializeField] private StaffRowView[] rows = new StaffRowView[0];
        [SerializeField] private TMP_Text emptyLabel = null;

        private readonly StringBuilder _text = new StringBuilder();
        private StaffService _staff;
        private StandService _stands;
        private int _confirmId = -1;
        private int _shownSecond = -1;

        public bool IsOpen => root.activeSelf;
        public bool ShowsButton => openButton.gameObject.activeSelf;
        public bool ShowsBadge => badge.activeSelf;
        public string ButtonText => openLabel.text;
        public string SummaryText => summaryLabel.text;
        public StaffRowView Row(int index) => rows[index];

        private void Start()
        {
            root.SetActive(false);
            if (!ServiceLocator.TryGet(out _staff))
            {
                openButton.gameObject.SetActive(false);
                return;
            }

            _stands = ServiceLocator.Get<StandService>();
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            findButton.onClick.AddListener(OnFind);
            for (int i = 0; i < rows.Length; i++) rows[i].Bind(i, OnMove, OnDismiss);
            EventBus.Subscribe<StaffPanelRequested>(OnRequested);
            EventBus.Subscribe<WorkerShown>(OnChanged);
            EventBus.Subscribe<WorkerGone>(OnChanged);
            EventBus.Subscribe<WorkerHired>(OnChanged);
            EventBus.Subscribe<WorkerAssigned>(OnChanged);
            EventBus.Subscribe<WorkerLevelUp>(OnChanged);
            EventBus.Subscribe<WageMissed>(OnChanged);
            EventBus.Subscribe<TierUpgraded>(OnChanged);
            RefreshButton();
        }

        private void OnDestroy()
        {
            if (_staff == null) return;
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            findButton.onClick.RemoveListener(OnFind);
            EventBus.Unsubscribe<StaffPanelRequested>(OnRequested);
            EventBus.Unsubscribe<WorkerShown>(OnChanged);
            EventBus.Unsubscribe<WorkerGone>(OnChanged);
            EventBus.Unsubscribe<WorkerHired>(OnChanged);
            EventBus.Unsubscribe<WorkerAssigned>(OnChanged);
            EventBus.Unsubscribe<WorkerLevelUp>(OnChanged);
            EventBus.Unsubscribe<WageMissed>(OnChanged);
            EventBus.Unsubscribe<TierUpgraded>(OnChanged);
        }

        public void Open()
        {
            EventBus.Publish(new SoundRequested(SoundId.UiClick));
            _confirmId = -1;
            root.SetActive(true);
            Refresh();
        }

        public void Close() => root.SetActive(false);

        // The payday countdown ticks while the list is open.
        private void Update()
        {
            if (_staff == null || !IsOpen) return;
            int second = Mathf.CeilToInt(_staff.SecondsToPayday);
            if (second != _shownSecond) RefreshSummary();
        }

        private void OnRequested(StaffPanelRequested evt) => Open();

        private void OnChanged<T>(T evt) where T : struct
        {
            RefreshButton();
            if (IsOpen) Refresh();
        }

        // Row buttons: Move opens the worker's card (pick a stand there); Dismiss asks once, then lets them go.
        private void OnMove(int row)
        {
            if (row >= _staff.Staff.Count) return;
            int id = _staff.Staff[row].Id;
            Close();
            EventBus.Publish(new WorkerCardRequested(id));
        }

        private void OnDismiss(int row)
        {
            if (row >= _staff.Staff.Count) return;
            int id = _staff.Staff[row].Id;
            if (_confirmId != id)
            {
                _confirmId = id;
                Refresh();
                return;
            }

            _confirmId = -1;
            _staff.TryDismiss(id);
        }

        // Flies the camera to the first job seeker and closes the list.
        private void OnFind()
        {
            if (_staff.Seekers.Count == 0) return;
            EventBus.Publish(new CameraFocusRequested(_staff.Seekers[0].Agent.Position));
            Close();
        }

        private void RefreshButton()
        {
            bool show = _staff.Slots > 0 || _staff.Staff.Count > 0;
            openButton.gameObject.SetActive(show);
            if (!show) return;
            openLabel.text = $"Staff {_staff.Staff.Count}/{_staff.Slots}";
            badge.SetActive(_staff.FreeSlots > 0 && _staff.Seekers.Count > 0);
        }

        private void Refresh()
        {
            RefreshSummary();
            int seekers = _staff.Seekers.Count;
            seekersLabel.text = seekers == 0 ? "No job seekers on the beach right now."
                : _staff.FreeSlots == 0 ? $"{seekers} job seekers are waiting, but every slot is taken."
                : $"{seekers} job seeker{(seekers == 1 ? " is" : "s are")} on the beach. Tap one with a ! to hire.";
            findButton.gameObject.SetActive(seekers > 0);
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < _staff.Staff.Count;
                rows[i].gameObject.SetActive(used);
                if (used) ShowRow(rows[i], _staff.Staff[i]);
            }

            emptyLabel.gameObject.SetActive(_staff.Staff.Count == 0);
        }

        private void RefreshSummary()
        {
            _shownSecond = Mathf.CeilToInt(_staff.SecondsToPayday);
            long wages = 0;
            for (int i = 0; i < _staff.Staff.Count; i++) wages += _staff.Wage(_staff.Staff[i].Model);
            _text.Clear().Append(_staff.Staff.Count).Append(" of ").Append(_staff.Slots).Append(" staff slots used");
            if (_staff.Staff.Count > 0)
            {
                _text.Append("\nNext payday in ").Append(_shownSecond).Append("s: ").Append(NumberFormatter.Format(wages)).Append(" coins");
            }

            summaryLabel.text = _text.ToString();
        }

        private void ShowRow(StaffRowView row, StaffMember member)
        {
            WorkerModel model = member.Model;
            string place = model.stand >= 0 ? _stands.Get(model.stand).Product.displayName : "No stand";
            string unpaid = model.IsUnhappy ? "  <color=#C0392B>Unpaid!</color>" : string.Empty;
            row.Show(_staff.Portraits.Get(model), $"<b>{model.name}</b>  Level {model.level}{unpaid}\n{place}, wage {NumberFormatter.Format(_staff.Wage(model))}",
                _confirmId == member.Id ? "Sure?" : "Dismiss");
        }
    }
}
