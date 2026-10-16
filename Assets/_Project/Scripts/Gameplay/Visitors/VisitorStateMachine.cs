using System;
using BeachTycoon.Core;
using BeachTycoon.Data;
using UnityEngine;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Visitor behaviour: Arriving -> WalkingToSpot -> Relaxing -> Queueing -> Buying -> (back to the spot
    /// or) Leaving. Patience only drains while waiting in line; running out costs popularity.
    /// </summary>
    public sealed class VisitorStateMachine
    {
        private readonly WorldNavigation _navigation;
        private readonly StandService _stands;
        private readonly VisitorConfig _config;
        private readonly System.Random _random;
        private readonly Action<VisitorAgent> _despawn;

        public VisitorStateMachine(WorldNavigation navigation, StandService stands, VisitorConfig config,
            System.Random random, Action<VisitorAgent> despawn)
        {
            _navigation = navigation;
            _stands = stands;
            _config = config;
            _random = random;
            _despawn = despawn;
        }

        public void Begin(VisitorAgent agent)
        {
            agent.Position = _navigation.GetEntry(agent.EntryIndex);
            _navigation.BuildPath(agent.Position, _navigation.GetBeachAccess(agent.Position), agent.Path);
            agent.State = VisitorState.Arriving;
        }

        public void Tick(VisitorAgent agent, float deltaTime)
        {
            float step = _config.walkSpeed * deltaTime;
            switch (agent.State)
            {
                case VisitorState.Arriving:
                    if (agent.Move(step, _config.arriveDistance)) GoToSpot(agent);
                    break;
                case VisitorState.WalkingToSpot:
                    if (agent.Move(step, _config.arriveDistance)) StartRelaxing(agent, RandomRelaxSeconds());
                    break;
                case VisitorState.Relaxing:
                    agent.Timer -= deltaTime;
                    if (agent.Timer <= 0f) DecideNext(agent);
                    break;
                case VisitorState.Queueing:
                case VisitorState.Buying:
                    TickInLine(agent, deltaTime, step);
                    break;
                case VisitorState.Leaving:
                    if (agent.Move(step, _config.arriveDistance)) _despawn(agent);
                    break;
            }
        }

        /// <summary>Called when the stand finished this visitor's order.</summary>
        public void OnPurchased(VisitorAgent agent)
        {
            agent.Purchases++;
            agent.StandIndex = -1;
            agent.QueueSlot = -1;
            EventBus.Publish(new VisitorPurchased(agent.Id, agent.Purchases));
            GoToSpot(agent);
        }

        /// <summary>Frees the relax spot and any queue place before the agent returns to the pool.</summary>
        public void Release(VisitorAgent agent)
        {
            if (agent.StandIndex >= 0)
            {
                _stands.Get(agent.StandIndex).RemoveOrder(agent.Id);
            }

            _navigation.ReleaseSpot(agent.SpotIndex);
        }

        private void GoToSpot(VisitorAgent agent)
        {
            if (agent.SpotIndex < 0)
            {
                agent.SpotIndex = _navigation.ReserveSpot(_random);
            }

            if (agent.SpotIndex < 0)
            {
                StartRelaxing(agent, RandomRelaxSeconds()); // beach is full: relax where they stand
                return;
            }

            _navigation.BuildPath(agent.Position, _navigation.GetSpot(agent.SpotIndex), agent.Path);
            agent.State = VisitorState.WalkingToSpot;
        }

        private static void StartRelaxing(VisitorAgent agent, float seconds)
        {
            agent.State = VisitorState.Relaxing;
            agent.Timer = seconds;
        }

        private void DecideNext(VisitorAgent agent)
        {
            if (agent.Purchases >= agent.MaxPurchases)
            {
                StartLeaving(agent);
                return;
            }

            StandController stand = PickStand(agent.Type);
            if (stand == null || !stand.TryEnqueueOrder(agent.Id, agent.Type.spendMultiplier))
            {
                StartRelaxing(agent, _config.retryQueueSeconds);
                return;
            }

            agent.StandIndex = stand.Index;
            agent.QueueSlot = -1;
            agent.Patience = agent.Type.patienceSeconds;
            agent.State = VisitorState.Queueing;
        }

        private void TickInLine(VisitorAgent agent, float deltaTime, float step)
        {
            StandController stand = _stands.Get(agent.StandIndex);
            int slot = stand.GetQueueIndex(agent.Id);
            if (slot < 0)
            {
                StartLeaving(agent); // order vanished (should not happen); fail safe
                return;
            }

            agent.State = slot == 0 ? VisitorState.Buying : VisitorState.Queueing;
            if (slot > 0)
            {
                agent.Patience -= deltaTime;
                if (agent.Patience <= 0f)
                {
                    stand.RemoveOrder(agent.Id);
                    agent.StandIndex = -1;
                    EventBus.Publish(new VisitorLeftUnserved(agent.Id));
                    StartLeaving(agent);
                    return;
                }
            }

            if (slot != agent.QueueSlot && _navigation.TryGetQueueSlot(stand.Index, slot, out Vector3 target))
            {
                agent.QueueSlot = slot;
                _navigation.BuildPath(agent.Position, target, agent.Path);
            }

            agent.Move(step, _config.arriveDistance);
        }

        private void StartLeaving(VisitorAgent agent)
        {
            _navigation.ReleaseSpot(agent.SpotIndex);
            agent.SpotIndex = -1;
            agent.StandIndex = -1;
            // Back to where the footpath meets the beach, then down the footpath.
            Vector3 entry = _navigation.GetEntry(agent.EntryIndex);
            Vector3 access = _navigation.GetBeachAccess(entry);
            _navigation.BuildPath(agent.Position, access, agent.Path);
            _navigation.AppendPath(access, entry, agent.Path);
            agent.State = VisitorState.Leaving;
        }

        private StandController PickStand(VisitorTypeDefinition type)
        {
            if (type.preferredProducts != null)
            {
                foreach (ProductDefinition product in type.preferredProducts)
                {
                    StandController preferred = product != null ? _stands.FindByProduct(product.id) : null;
                    if (preferred != null && !preferred.IsFull) return preferred;
                }
            }

            int count = _stands.Count;
            int start = count > 0 ? _random.Next(count) : 0;
            for (int i = 0; i < count; i++)
            {
                StandController stand = _stands.Get((start + i) % count);
                if (!stand.IsFull) return stand;
            }

            return null;
        }

        private float RandomRelaxSeconds()
        {
            Vector2 range = _config.relaxSecondsRange;
            return Mathf.Lerp(range.x, range.y, (float)_random.NextDouble());
        }
    }
}
