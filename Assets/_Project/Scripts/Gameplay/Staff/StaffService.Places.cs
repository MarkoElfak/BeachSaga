using System;
using BeachTycoon.Core;
using BeachTycoon.Services;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Who works where: places at a stand (two at a restaurant, workers fill the seats from the left and the character
    /// takes the last one), the speed everyone there serves at together, and who collects the coins.
    /// </summary>
    public sealed partial class StaffService
    {
        /// <summary>The first worker assigned to a stand (walking there or at work), or null.</summary>
        public StaffMember WorkerAt(int standIndex)
        {
            for (int i = 0; i < _staff.Count; i++) if (_staff[i].Model.stand == standIndex) return _staff[i];
            return null;
        }

        /// <summary>Workers assigned to a stand (walking there or at work), the character not counted.</summary>
        public int CountAt(int standIndex)
        {
            int count = 0;
            for (int i = 0; i < _staff.Count; i++) if (_staff[i].Model.stand == standIndex) count++;
            return count;
        }

        /// <summary>Room for one more person: fewer workers (plus the character) there than the stand has places.</summary>
        public bool IsStandFree(int standIndex) =>
            standIndex >= 0 && standIndex < _stands.Count
            && CountAt(standIndex) + (standIndex == CharacterStand ? 1 : 0) < _stands.Get(standIndex).Product.workerSlots;

        // Workers at the same stand take its seats in hiring order.
        private int SeatOf(StaffMember member)
        {
            int seat = 0;
            for (int i = 0; i < _staff.Count && _staff[i] != member; i++) if (_staff[i].Model.stand == member.Model.stand) seat++;
            return Math.Min(seat, _spots.LastSeat(member.Model.stand));
        }

        /// <summary>
        /// Sets a stand's serving speed and head count from everyone at the counter: the character's own bonus and each
        /// worker's skill bonus (slower while unpaid), several people together as a team, or half speed with nobody there.
        /// </summary>
        private void ApplySpeed(int standIndex)
        {
            StandController stand = _stands.Get(standIndex);
            stand.UnstaffedSpeed = _config.unstaffedServeSpeed;
            float fastest = 0f, others = 0f;
            int people = 0;
            if (_characters.IsWorking && _characters.StandIndex == standIndex) AddSpeed(_characters.ServeSpeed, ref fastest, ref others, ref people);
            for (int i = 0; i < _staff.Count; i++)
            {
                StaffMember worker = _staff[i];
                if (worker.Model.stand != standIndex || !worker.IsWorking) continue;
                float speed = StaffMath.ServeSpeed(_config, worker.Model, stand.Product.skill, IsFromHome(worker.Model));
                AddSpeed(speed, ref fastest, ref others, ref people);
            }

            stand.People = people;
            stand.ServeSpeed = people > 0 ? StaffMath.TeamSpeed(_config, fastest, others) : stand.UnstaffedSpeed;
        }

        private static void AddSpeed(float speed, ref float fastest, ref float others, ref int people)
        {
            people++;
            others += Mathf.Min(speed, fastest);
            fastest = Mathf.Max(speed, fastest);
        }

        // A worker of the collecting level at the counter picks up the stand's coins (what managers did before).
        private void RefreshCollector(int standIndex)
        {
            StandController stand = _stands.Get(standIndex);
            bool collects = false;
            for (int i = 0; i < _staff.Count && !collects; i++)
            {
                StaffMember worker = _staff[i];
                collects = worker.Model.stand == standIndex && worker.IsWorking && CollectsCoins(worker.Model);
            }

            if (collects == stand.Model.manager) return;
            stand.Model.manager = collects;
            if (collects) EventBus.Publish(new ManagerHired(standIndex, stand.Product.id));
            else EventBus.Publish(new ManagerLeft(standIndex));
        }

        private StaffMember LastWorkerAt(int standIndex)
        {
            for (int i = _staff.Count - 1; i >= 0; i--) if (_staff[i].Model.stand == standIndex) return _staff[i];
            return null;
        }
    }
}
