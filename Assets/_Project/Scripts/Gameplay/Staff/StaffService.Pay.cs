using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Paydays (unpaid workers slow down, warn, then quit) and job seekers arriving while a slot is free.</summary>
    public sealed partial class StaffService
    {
        /// <summary>Seconds of play until the next payday.</summary>
        public float SecondsToPayday => _data.meta.wageTimer < 0f ? _config.paySeconds : _data.meta.wageTimer;

        /// <summary>Pays every wage now. A worker whose wage cannot be paid misses it; too many in a row and they quit.</summary>
        public void PayWages()
        {
            long paid = 0;
            for (int i = _staff.Count - 1; i >= 0; i--)
            {
                StaffMember member = _staff[i];
                WorkerModel model = member.Model;
                long wage = Wage(model);
                if (_economy.TrySpend(wage))
                {
                    paid += wage;
                    if (model.missedPays == 0) continue;
                    model.missedPays = 0;
                    if (model.stand >= 0) ApplySpeed(model.stand); // happy again: full speed
                    continue;
                }

                model.missedPays++;
                if (model.missedPays >= _config.quitAfterMissedPays)
                {
                    string name = model.name;
                    Remove(member);
                    EventBus.Publish(new WorkerQuit(name));
                    continue;
                }

                if (model.stand >= 0) ApplySpeed(model.stand);
                EventBus.Publish(new WageMissed(member.Id, model.missedPays));
            }

            if (paid > 0) EventBus.Publish(new WagesPaid(paid));
        }

        /// <summary>Brings job seekers until the beach has its full set (while a slot is free); returns how many came.</summary>
        public int FillSeekers(bool walkIn = true)
        {
            int count = 0;
            for (; FreeSlots > 0 && _seekers.Count < _config.maxJobSeekers; count++) AddSeeker(walkIn);

            if (count > 0) EventBus.Publish(new JobSeekersArrived(count));
            return count;
        }

        private void TickWages(float deltaTime)
        {
            if (_staff.Count == 0) return;
            if (_data.meta.wageTimer < 0f) _data.meta.wageTimer = _config.paySeconds;
            _data.meta.wageTimer -= deltaTime;
            if (_data.meta.wageTimer > 0f) return;
            _data.meta.wageTimer = _config.paySeconds;
            PayWages();
        }

        // One more seeker now and then, up to the full set, while someone could still be hired.
        private void TickSeekers(float deltaTime)
        {
            if (FreeSlots <= 0 || _seekers.Count >= _config.maxJobSeekers) return;
            if (_data.meta.seekerTimer < 0f) _data.meta.seekerTimer = _config.seekerArrivalSeconds;
            _data.meta.seekerTimer -= deltaTime;
            if (_data.meta.seekerTimer > 0f) return;
            _data.meta.seekerTimer = _config.seekerArrivalSeconds;
            AddSeeker(true);
            EventBus.Publish(new JobSeekersArrived(1));
        }

        // A bigger main building: more slots, and a full set of job seekers comes to apply.
        private void OnTierUpgraded(TierUpgraded evt)
        {
            if (evt.LotIndex == BuildingManager.MainLotIndex) FillSeekers();
        }

        private void AddSeeker(bool walkIn)
        {
            WorkerModel model = _factory.Create(_data.meta.nextWorkerId++, _staff, _seekers);
            _data.jobSeekers.Add(model);
            ShowSeeker(new StaffMember(model, true), walkIn);
        }

        // Seekers wait on the free beach spot nearest the main building; new ones walk in from the footpath.
        private void ShowSeeker(StaffMember member, bool walkIn)
        {
            _seekers.Add(member);
            Vector3 waiting = _spots.Waiting();
            member.Spot = _navigation.ReserveSpotNear(waiting);
            Vector3 target = member.Spot >= 0 ? _navigation.GetSpot(member.Spot) : waiting;
            member.Agent.Facing = Vector3.back;
            member.ArriveFacing = Vector3.back;
            if (walkIn && _navigation.EntryCount > 0)
            {
                Vector3 entry = _navigation.GetEntry(_random.Next(_navigation.EntryCount));
                Vector3 access = _navigation.GetBeachAccess(entry);
                member.Agent.Position = entry;
                _navigation.BuildPath(entry, access, member.Agent.Path);
                _navigation.AppendPath(access, target, member.Agent.Path);
            }
            else
            {
                member.Agent.Position = target;
            }

            EventBus.Publish(new WorkerShown(member.Id));
        }
    }
}
