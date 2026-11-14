using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using TMPro;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>The card's status line, the stand buttons (who is where, the skill each stand needs) and live updates.</summary>
    public sealed partial class WorkerCardView
    {
        private string PayPeriod()
        {
            float seconds = _staff.Config.paySeconds;
            return Mathf.Approximately(seconds, 60f) ? "a minute" : $"every {seconds:0} seconds";
        }

        private string Status(WorkerModel model, bool seeker)
        {
            if (seeker) return "Looking for a job";
            if (model.stand < 0) return "Waiting for a free stand";
            string product = _stands.Get(model.stand).Product.displayName;
            if (model.IsUnhappy) return $"Unpaid! Works slower at {product}";
            if (!_member.IsWorking) return $"Walking over to {product}";
            return _staff.CollectsCoins(model) ? $"Works at {product} and collects its coins" : $"Works at {product}";
        }

        // One button per stand: free ones hire (or move) them there, the rest say who is already there.
        private void RefreshStands(WorkerModel model, bool seeker)
        {
            bool canHire = !seeker || _staff.FreeSlots > 0;
            hintLabel.text = !canHire
                ? "Every staff slot is taken. Upgrade the main building for more."
                : seeker ? "Hire them for a stand:" : "Move them to another stand:";
            for (int i = 0; i < standButtons.Length; i++)
            {
                bool exists = i < _stands.Count;
                standButtons[i].gameObject.SetActive(exists);
                if (!exists) continue;
                StandController stand = _stands.Get(i);
                StaffSkill skill = stand.Product.skill;
                string who = WhoWorksAt(i, model);
                standButtons[i].interactable = canHire && who == null;
                var label = standButtons[i].GetComponentInChildren<TMP_Text>();
                label.text = $"{stand.Product.NameAt(_stands.MainTier)}\n<size=75%>{(who ?? $"{StaffSkills.Name(skill)} {model.Stars(skill)}/{StaffSkills.MaxStars}")}</size>";
            }
        }

        // Null while the stand has room for them; otherwise who is already there (up to two at a restaurant).
        private string WhoWorksAt(int stand, WorkerModel model)
        {
            if (model.stand == stand) return "Works here";
            if (_staff.IsStandFree(stand)) return null;
            string first = _characters.HasJoined && _characters.StandIndex == stand ? _characters.Character.displayName : null;
            string second = null;
            foreach (StaffMember other in _staff.Staff)
            {
                if (other.Model.stand != stand) continue;
                if (first == null) first = other.Model.name;
                else if (second == null) second = other.Model.name;
            }

            return second != null ? $"{first} and {second}" : $"{first ?? "Someone"} is here";
        }

        private void Subscribe(bool on)
        {
            if (on)
            {
                EventBus.Subscribe<WorkerCardRequested>(OnRequested);
                EventBus.Subscribe<WorkerAssigned>(OnStaffChanged);
                EventBus.Subscribe<WorkerStartedWork>(OnStaffChanged);
                EventBus.Subscribe<WorkerLevelUp>(OnStaffChanged);
                EventBus.Subscribe<WageMissed>(OnStaffChanged);
                EventBus.Subscribe<CharacterAssigned>(OnStaffChanged);
                EventBus.Subscribe<WorkerGone>(OnGone);
            }
            else
            {
                EventBus.Unsubscribe<WorkerCardRequested>(OnRequested);
                EventBus.Unsubscribe<WorkerAssigned>(OnStaffChanged);
                EventBus.Unsubscribe<WorkerStartedWork>(OnStaffChanged);
                EventBus.Unsubscribe<WorkerLevelUp>(OnStaffChanged);
                EventBus.Unsubscribe<WageMissed>(OnStaffChanged);
                EventBus.Unsubscribe<CharacterAssigned>(OnStaffChanged);
                EventBus.Unsubscribe<WorkerGone>(OnGone);
            }
        }

        private void OnRequested(WorkerCardRequested evt) => Open(evt.WorkerId);

        private void OnStaffChanged<T>(T evt) where T : struct
        {
            if (IsOpen) Refresh();
        }

        private void OnGone(WorkerGone evt)
        {
            if (IsOpen && _member != null && _member.Id == evt.WorkerId) Close();
        }
    }
}
