using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>Walking to stands, serving speed, levels from sales, coin collecting and sharing stands with the character.</summary>
    public sealed partial class StaffService
    {
        private const float WaitingGap = 0.9f;

        private void TickWalk(StaffMember member, float deltaTime)
        {
            if (!member.IsWalking) return;
            if (!member.Agent.Move(_database.visitors.walkSpeed * deltaTime, _database.visitors.arriveDistance)) return;
            member.Agent.Facing = member.ArriveFacing;
            if (!member.IsSeeker && member.Model.stand >= 0) StartWork(member);
        }

        private void WalkToWork(StaffMember member) =>
            _spots.BuildWalk(member.Agent.Position, member.Model.stand, member.Agent.Path, out member.ArriveFacing);

        // Loaded at their stand: no walk.
        private void PlaceAtWork(StaffMember member)
        {
            member.Agent.Position = _spots.Work(member.Model.stand, out member.ArriveFacing);
            member.Agent.Facing = member.ArriveFacing;
            member.IsWorking = true;
        }

        // No stand to go to: wait in front of the main building, side by side.
        private void PlaceWaiting(StaffMember member)
        {
            member.Agent.Position = _spots.Waiting(-WaitingGap * (1 + _staff.IndexOf(member)));
            member.Agent.Facing = Vector3.back;
        }

        private void WalkToWaiting(StaffMember member)
        {
            _navigation.BuildPath(member.Agent.Position, _spots.Waiting(-WaitingGap * (1 + _staff.IndexOf(member))), member.Agent.Path);
            member.ArriveFacing = Vector3.back;
        }

        private void StartWork(StaffMember member)
        {
            member.IsWorking = true;
            int stand = member.Model.stand;
            ApplySpeed(stand);
            RefreshCollector(stand);
            EventBus.Publish(new WorkerStartedWork(member.Id, stand));
        }

        private void StopWork(StaffMember member)
        {
            bool wasWorking = member.IsWorking;
            member.IsWorking = false;
            member.Agent.Path.Clear();
            int stand = member.Model.stand;
            if (!wasWorking || stand < 0 || stand >= _stands.Count) return;
            ApplySpeed(stand);
            RefreshCollector(stand);
        }

        /// <summary>
        /// Sets a stand's serving speed from whoever works there: the character's own bonus, a worker's skill bonus
        /// (slower while unpaid), or half speed with nobody at the counter.
        /// </summary>
        private void ApplySpeed(int standIndex)
        {
            StandController stand = _stands.Get(standIndex);
            stand.UnstaffedSpeed = _config.unstaffedServeSpeed;
            if (_characters.IsWorking && _characters.StandIndex == standIndex) return;
            StaffMember worker = WorkerAt(standIndex);
            stand.ServeSpeed = worker != null && worker.IsWorking
                ? StaffMath.ServeSpeed(_config, worker.Model, stand.Product.skill, IsFromHome(worker.Model))
                : stand.UnstaffedSpeed;
        }

        // A worker of the collecting level at the counter picks up the stand's coins (what managers did before).
        private void RefreshCollector(int standIndex)
        {
            StandController stand = _stands.Get(standIndex);
            StaffMember worker = WorkerAt(standIndex);
            bool collects = worker != null && worker.IsWorking && CollectsCoins(worker.Model);
            if (collects == stand.Model.manager) return;
            stand.Model.manager = collects;
            if (collects) EventBus.Publish(new ManagerHired(standIndex, stand.Product.id));
            else EventBus.Publish(new ManagerLeft(standIndex));
        }

        // Each sale at their stand counts toward the next level, which adds a star in the stand's skill.
        private void OnSaleCompleted(SaleCompleted evt)
        {
            StaffMember worker = WorkerAt(evt.StandIndex);
            if (worker == null || !worker.IsWorking || !StaffMath.AddSale(_config, worker.Model)) return;
            int skill = (int)_stands.Get(evt.StandIndex).Product.skill;
            worker.Model.skills[skill] = Math.Min(StaffSkills.MaxStars, worker.Model.Stars((StaffSkill)skill) + 1);
            ApplySpeed(evt.StandIndex);
            RefreshCollector(evt.StandIndex);
            EventBus.Publish(new WorkerLevelUp(worker.Id, worker.Model.level));
        }

        private void OnStandCreated(StandCreated evt) => ApplySpeed(evt.StandIndex);

        // Their stand moved (on its own, or with its lot's default row): walk to its new side.
        private void OnObjectMoved(ObjectMoved evt)
        {
            for (int i = 0; i < _staff.Count; i++)
            {
                StaffMember member = _staff[i];
                if (member.Model.stand < 0 || !StandSpots.Moves(evt, _stands.Get(member.Model.stand))) continue;
                StopWork(member);
                WalkToWork(member);
            }
        }

        // The character took a worker's stand: the two swap, or the worker waits when the character had none.
        private void OnCharacterAssigned(CharacterAssigned evt)
        {
            int previous = _characterStand;
            _characterStand = evt.StandIndex;
            StaffMember worker = WorkerAt(evt.StandIndex);
            if (worker == null) return;
            StopWork(worker);
            bool swap = previous >= 0 && previous < _stands.Count && previous != evt.StandIndex && WorkerAt(previous) == null;
            worker.Model.stand = swap ? previous : -1;
            RefreshCollector(evt.StandIndex);
            if (swap) WalkToWork(worker);
            else WalkToWaiting(worker);
            EventBus.Publish(new WorkerAssigned(worker.Id, worker.Model.stand));
        }

        private void OnCharacterJoined(CharacterJoined evt)
        {
            _characterStand = CharacterStand;
            ResolveCharacterClash();
        }

        // A worker already runs the character's stand (a manager from an older save): the character moves on.
        private void ResolveCharacterClash()
        {
            int stand = CharacterStand;
            StaffMember worker = stand >= 0 ? WorkerAt(stand) : null;
            if (worker == null) return;
            for (int i = 0; i < _stands.Count; i++)
            {
                if (!IsStandFree(i)) continue;
                _characters.TrySendTo(i); // CharacterAssigned: the stand is free, so nobody swaps
                ApplySpeed(stand);
                return;
            }

            StopWork(worker);
            worker.Model.stand = -1;
            RefreshCollector(stand);
            PlaceWaiting(worker);
        }
    }
}
