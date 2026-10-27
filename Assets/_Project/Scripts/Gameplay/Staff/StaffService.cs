using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Hired staff (M18). Job seekers wait on the beach with a badge; hired, a worker walks to a stand and runs it:
    /// the stand serves at full speed plus their skill bonus (half speed with nobody there). One person per stand,
    /// the player's character included. Workers level up from sales, are paid every payday and, unpaid, slow down
    /// and finally quit. The main building's tier sets how many can be hired.
    /// </summary>
    public sealed partial class StaffService : ITickable, IDisposable
    {
        private readonly GameDatabase _database;
        private readonly StaffConfig _config;
        private readonly GameSaveData _data;
        private readonly StandService _stands;
        private readonly BuildingManager _buildings;
        private readonly CharacterService _characters;
        private readonly WorldNavigation _navigation;
        private readonly IEconomyService _economy;
        private readonly StandSpots _spots;
        private readonly WorkerFactory _factory;
        private readonly System.Random _random;
        private readonly List<StaffMember> _staff = new List<StaffMember>();
        private readonly List<StaffMember> _seekers = new List<StaffMember>();
        private int _characterStand;

        public StaffService(GameDatabase database, GameSaveData data, StandService stands, BuildingManager buildings,
            CultureService cultures, CharacterService characters, PlacementService placement, WorldNavigation navigation,
            IEconomyService economy, int seed = 4321)
        {
            _database = database;
            _config = database.staff;
            _data = data;
            _stands = stands;
            _buildings = buildings;
            _characters = characters;
            _navigation = navigation;
            _economy = economy;
            _random = new System.Random(seed);
            _spots = new StandSpots(stands, placement, navigation, database.world);
            _factory = new WorkerFactory(_config, cultures.All, _random);
            Portraits = new WorkerPortraits(database.palette, cultures);
            _characterStand = CharacterStand;

            EventBus.Subscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Subscribe<StandCreated>(OnStandCreated);
            EventBus.Subscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Subscribe<CharacterAssigned>(OnCharacterAssigned);
            EventBus.Subscribe<CharacterJoined>(OnCharacterJoined);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
            Restore();
        }

        public IReadOnlyList<StaffMember> Staff => _staff;
        public IReadOnlyList<StaffMember> Seekers => _seekers;
        public StaffConfig Config => _config;
        public WorkerPortraits Portraits { get; }

        /// <summary>Workers the main building allows now.</summary>
        public int Slots => _config.SlotsFor(_buildings.MainTier);
        public int FreeSlots => Math.Max(0, Slots - _staff.Count);

        private int CharacterStand => _characters.HasJoined ? _characters.StandIndex : -1;

        /// <summary>A worker or job seeker by id, or null.</summary>
        public StaffMember Find(int workerId) => Find(_staff, workerId) ?? Find(_seekers, workerId);

        /// <summary>The worker assigned to a stand (walking there or at work), or null.</summary>
        public StaffMember WorkerAt(int standIndex)
        {
            for (int i = 0; i < _staff.Count; i++) if (_staff[i].Model.stand == standIndex) return _staff[i];
            return null;
        }

        /// <summary>Nobody works there: no worker and not the player's character.</summary>
        public bool IsStandFree(int standIndex) =>
            standIndex >= 0 && standIndex < _stands.Count && WorkerAt(standIndex) == null && standIndex != CharacterStand;

        public long Wage(WorkerModel worker) => StaffMath.Wage(_config, worker);

        /// <summary>All wages per second (offline earnings pay them too).</summary>
        public double WagesPerSecond
        {
            get
            {
                long total = 0;
                for (int i = 0; i < _staff.Count; i++) total += Wage(_staff[i].Model);
                return total / (double)_config.paySeconds;
            }
        }

        public bool IsFromHome(WorkerModel worker) => worker.cultureId == _data.meta.cultureId;
        public bool CollectsCoins(WorkerModel worker) => worker.level >= _config.collectCoinsLevel;
        public int SalesToNextLevel(WorkerModel worker) => StaffMath.SalesToNextLevel(_config, worker.level);

        public bool CanHire(int workerId, int standIndex) =>
            Find(_seekers, workerId) != null && FreeSlots > 0 && IsStandFree(standIndex);

        /// <summary>Hires a job seeker for a free stand; they walk over and start there.</summary>
        public bool TryHire(int workerId, int standIndex)
        {
            if (!CanHire(workerId, standIndex)) return false;
            StaffMember member = Find(_seekers, workerId);
            _navigation.ReleaseSpot(member.Spot);
            member.Spot = -1;
            _seekers.Remove(member);
            _data.jobSeekers.Remove(member.Model);
            member.IsSeeker = false;
            member.Model.stand = standIndex;
            member.Model.missedPays = 0;
            _staff.Add(member);
            _data.staff.Add(member.Model);
            WalkToWork(member);
            EventBus.Publish(new WorkerHired(workerId, standIndex));
            EventBus.Publish(new WorkerAssigned(workerId, standIndex));
            return true;
        }

        public bool CanMove(int workerId, int standIndex) => Find(_staff, workerId) != null && IsStandFree(standIndex);

        /// <summary>Sends a worker to another free stand.</summary>
        public bool TryMove(int workerId, int standIndex)
        {
            if (!CanMove(workerId, standIndex)) return false;
            StaffMember member = Find(_staff, workerId);
            StopWork(member);
            member.Model.stand = standIndex;
            WalkToWork(member);
            EventBus.Publish(new WorkerAssigned(workerId, standIndex));
            return true;
        }

        /// <summary>Lets a worker go; their stand drops back to half speed.</summary>
        public bool TryDismiss(int workerId)
        {
            StaffMember member = Find(_staff, workerId);
            if (member == null) return false;
            Remove(member);
            return true;
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _staff.Count; i++) TickWalk(_staff[i], deltaTime);
            for (int i = 0; i < _seekers.Count; i++) TickWalk(_seekers[i], deltaTime);
            TickWages(deltaTime);
            TickSeekers(deltaTime);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<SaleCompleted>(OnSaleCompleted);
            EventBus.Unsubscribe<StandCreated>(OnStandCreated);
            EventBus.Unsubscribe<ObjectMoved>(OnObjectMoved);
            EventBus.Unsubscribe<CharacterAssigned>(OnCharacterAssigned);
            EventBus.Unsubscribe<CharacterJoined>(OnCharacterJoined);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
            Portraits.Dispose();
        }

        // Saved staff are at work already; managers from an older save get a name, looks and stars first.
        private void Restore()
        {
            for (int i = 0; i < _stands.Count; i++) _stands.Get(i).UnstaffedSpeed = _config.unstaffedServeSpeed;
            foreach (WorkerModel model in _data.staff)
            {
                var member = new StaffMember(model, false);
                _staff.Add(member);
                if (model.stand >= _stands.Count) model.stand = -1;
                if (string.IsNullOrEmpty(model.name)) FillFormerManager(model);
                if (model.stand >= 0) PlaceAtWork(member);
                else PlaceWaiting(member);
            }

            foreach (WorkerModel model in _data.jobSeekers) ShowSeeker(new StaffMember(model, true), false);
            ResolveCharacterClash();
            for (int i = 0; i < _stands.Count; i++)
            {
                ApplySpeed(i);
                RefreshCollector(i);
            }

            if (_staff.Count == 0 && _seekers.Count == 0) FillSeekers(false);
        }

        private void FillFormerManager(WorkerModel model)
        {
            _factory.Fill(model, _staff, _seekers);
            if (model.stand < 0) return;
            int skill = (int)_stands.Get(model.stand).Product.skill;
            model.skills[skill] = Math.Max(model.skills[skill], model.level);
        }

        // Removes a worker for good (dismissed or quit): their stand goes back to unstaffed.
        private void Remove(StaffMember member)
        {
            StopWork(member);
            int stand = member.Model.stand;
            member.Model.stand = -1;
            _staff.Remove(member);
            _data.staff.Remove(member.Model);
            if (stand >= 0) RefreshCollector(stand);
            Portraits.Forget(member.Id);
            EventBus.Publish(new WorkerGone(member.Id));
        }

        private static StaffMember Find(List<StaffMember> members, int workerId)
        {
            for (int i = 0; i < members.Count; i++) if (members[i].Id == workerId) return members[i];
            return null;
        }
    }
}
