using BeachTycoon.Core;
using BeachTycoon.Gameplay;

namespace BeachTycoon.UI
{
    /// <summary>Staff news: job seekers arriving, hires, level ups, unpaid wages and quitting.</summary>
    public sealed partial class ToastView
    {
        private StaffService _staff;

        private void SubscribeStaff(bool on)
        {
            if (on && !ServiceLocator.TryGet(out _staff)) return;
            if (on)
            {
                EventBus.Subscribe<JobSeekersArrived>(OnSeekersArrived);
                EventBus.Subscribe<WorkerHired>(OnHired);
                EventBus.Subscribe<WorkerLevelUp>(OnLevelUp);
                EventBus.Subscribe<WageMissed>(OnWageMissed);
                EventBus.Subscribe<WorkerQuit>(OnQuit);
            }
            else
            {
                EventBus.Unsubscribe<JobSeekersArrived>(OnSeekersArrived);
                EventBus.Unsubscribe<WorkerHired>(OnHired);
                EventBus.Unsubscribe<WorkerLevelUp>(OnLevelUp);
                EventBus.Unsubscribe<WageMissed>(OnWageMissed);
                EventBus.Unsubscribe<WorkerQuit>(OnQuit);
            }
        }

        private void OnSeekersArrived(JobSeekersArrived evt) =>
            Show(evt.Count == 1 ? "A job seeker is on the beach! Tap the ! to hire." : $"{evt.Count} job seekers are on the beach! Tap a ! to hire.");

        private void OnHired(WorkerHired evt)
        {
            StaffMember worker = _staff.Find(evt.WorkerId);
            if (worker != null) Show($"{worker.Model.name} works at {ServiceLocator.Get<StandService>().Get(evt.StandIndex).Product.displayName} now!");
        }

        private void OnLevelUp(WorkerLevelUp evt)
        {
            StaffMember worker = _staff.Find(evt.WorkerId);
            if (worker == null) return;
            bool collects = evt.Level == _staff.Config.collectCoinsLevel;
            Show($"{worker.Model.name} reached level {evt.Level}!{(collects ? " They collect the coins now." : string.Empty)}");
        }

        private void OnWageMissed(WageMissed evt)
        {
            StaffMember worker = _staff.Find(evt.WorkerId);
            if (worker == null) return;
            string name = worker.Model.name;
            Show(evt.Missed >= _staff.Config.warnAfterMissedPays
                ? $"{name} will quit if the next wage is missed!"
                : $"Not enough coins to pay {name}. They work slower until paid.");
        }

        private void OnQuit(WorkerQuit evt) => Show($"{evt.Name} quit: their wage was not paid.");
    }
}
