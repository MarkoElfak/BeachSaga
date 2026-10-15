using System;
using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Repeat visitors with enough purchases may ask to stay. If accepted and a free lot and slot exist,
    /// a house is built for them; each house then produces rent on a timer. Quests are handled by QuestService.
    /// </summary>
    public sealed class NeighbourService : ITickable, IDisposable
    {
        private readonly GameDatabase _database;
        private readonly BuildingManager _buildings;
        private readonly ProgressionService _progression;
        private readonly List<NeighbourModel> _neighbours;
        private readonly System.Random _random;
        private string _pendingName;
        private float _pendingTimer;

        public NeighbourService(GameDatabase database, BuildingManager buildings, ProgressionService progression,
            List<NeighbourModel> neighbours)
        {
            _database = database;
            _buildings = buildings;
            _progression = progression;
            _neighbours = neighbours;
            _random = new System.Random(database.visitors.randomSeed + neighbours.Count);
            EventBus.Subscribe<VisitorPurchased>(OnVisitorPurchased);
        }

        public int Count => _neighbours.Count;
        public bool HasPendingRequest => _pendingName != null;
        public string PendingName => _pendingName;

        /// <summary>A free house lot exists and the current tier has a neighbour slot left.</summary>
        public bool HasRoom => _neighbours.Count < _progression.NeighbourSlots && _buildings.FindFreeHouseLot() >= 0;

        public NeighbourModel Get(int index) => _neighbours[index];

        public int FindByLot(int lotIndex)
        {
            for (int i = 0; i < _neighbours.Count; i++)
            {
                if (_neighbours[i].lotIndex == lotIndex) return i;
            }

            return -1;
        }

        public bool Accept()
        {
            if (!HasPendingRequest || !HasRoom)
            {
                Close(false);
                return false;
            }

            NeighbourConfig config = _database.neighbours;
            int lot = _buildings.FindFreeHouseLot();
            int variants = config.housePrefabs != null ? config.housePrefabs.Length : 1;
            _buildings.TryBuildHouse(lot, _random.Next(Math.Max(1, variants)));
            _neighbours.Add(new NeighbourModel { name = _pendingName, lotIndex = lot });
            Close(true);
            EventBus.Publish(new NeighbourMovedIn(_neighbours.Count - 1, lot));
            return true;
        }

        public void Decline() => Close(false);

        public void Tick(float deltaTime)
        {
            if (HasPendingRequest)
            {
                _pendingTimer -= deltaTime;
                if (_pendingTimer <= 0f) Close(false);
            }

            NeighbourConfig config = _database.neighbours;
            for (int i = 0; i < _neighbours.Count; i++)
            {
                NeighbourModel neighbour = _neighbours[i];
                if (_buildings.GetLot(neighbour.lotIndex).state != LotState.Built) continue;
                neighbour.rentTimer += deltaTime;
                if (neighbour.rentTimer < config.rentIntervalSeconds) continue;

                neighbour.rentTimer -= config.rentIntervalSeconds;
                long rent = (long)Math.Round(config.rentPerMinute * config.rentIntervalSeconds / 60.0);
                EventBus.Publish(new RentDue(neighbour.lotIndex, rent));
            }
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<VisitorPurchased>(OnVisitorPurchased);
        }

        private void OnVisitorPurchased(VisitorPurchased evt)
        {
            NeighbourConfig config = _database.neighbours;
            bool eligible = evt.Purchases == config.minPurchasesToAsk && !HasPendingRequest && HasRoom;
            if (!eligible || _random.NextDouble() >= config.askChance)
            {
                return;
            }

            _pendingName = PickName(config);
            _pendingTimer = config.requestTimeoutSeconds;
            EventBus.Publish(new NeighbourRequested(_pendingName));
        }

        private string PickName(NeighbourConfig config)
        {
            if (config.names == null || config.names.Length == 0) return "Neighbour " + (_neighbours.Count + 1);
            string name = config.names[_neighbours.Count % config.names.Length];
            return _neighbours.Count < config.names.Length ? name : name + " " + (_neighbours.Count / config.names.Length + 1);
        }

        private void Close(bool accepted)
        {
            if (!HasPendingRequest) return;
            _pendingName = null;
            EventBus.Publish(new NeighbourRequestClosed(accepted));
        }
    }
}
