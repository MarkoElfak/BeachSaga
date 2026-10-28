using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Walking to stands, serving speed, levels from sales, coin collecting and sharing stands with the character.
    /// </summary>
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
            _spots.BuildWalk(member.Agent.Position, member.Model.stand, SeatOf(member), member.Agent.Path, out member.ArriveFacing);

        // Loaded at their stand: no walk.
        private void PlaceAtWork(StaffMember member)
        {
            member.Agent.Position = _spots.Work(member.Model.stand, SeatOf(member), out member.ArriveFacing);
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

        // Each sale at their stand counts toward everyone's next level there, which adds a star in the stand's skill.
        private void OnSaleCompleted(SaleCompleted evt)
        {
            for (int i = 0; i < _staff.Count; i++)
            {
                StaffMember worker = _staff[i];
                if (worker.Model.stand != evt.StandIndex || !worker.IsWorking || !StaffMath.AddSale(_config, worker.Model)) continue;
                int skill = (int)_stands.Get(evt.StandIndex).Product.skill;
                worker.Model.skills[skill] = Math.Min(StaffSkills.MaxStars, worker.Model.Stars((StaffSkill)skill) + 1);
                ApplySpeed(evt.StandIndex);
                RefreshCollector(evt.StandIndex);
                EventBus.Publish(new WorkerLevelUp(worker.Id, worker.Model.level));
            }
        }

        private void OnStandCreated(StandCreated evt) => ApplySpeed(evt.StandIndex);

        private void OnCharacterStartedWork(CharacterStartedWork evt) => ApplySpeed(evt.StandIndex);

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

        // The character took a place at a stand. When that leaves too many people there, the last worker swaps to the
        // character's old stand, or waits when there is no room there. The stand they left serves without them.
        private void OnCharacterAssigned(CharacterAssigned evt)
        {
            int previous = _characterStand;
            _characterStand = evt.StandIndex;
            if (previous >= 0 && previous < _stands.Count) ApplySpeed(previous);
            StaffMember worker = LastWorkerAt(evt.StandIndex);
            if (worker == null || CountAt(evt.StandIndex) < _stands.Get(evt.StandIndex).Product.workerSlots) return;
            StopWork(worker);
            bool swap = previous >= 0 && previous < _stands.Count && previous != evt.StandIndex && IsStandFree(previous);
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

        // Workers already fill the character's stand (a manager from an older save): the character moves on.
        private void ResolveCharacterClash()
        {
            int stand = CharacterStand;
            if (stand < 0 || CountAt(stand) < _stands.Get(stand).Product.workerSlots) return;
            for (int i = 0; i < _stands.Count; i++)
            {
                if (!IsStandFree(i)) continue;
                _characters.TrySendTo(i); // CharacterAssigned: the stand has room, so nobody swaps
                ApplySpeed(stand);
                return;
            }

            StaffMember worker = LastWorkerAt(stand);
            StopWork(worker);
            worker.Model.stand = -1;
            RefreshCollector(stand);
            PlaceWaiting(worker);
        }
    }
}
