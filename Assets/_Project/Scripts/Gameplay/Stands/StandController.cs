using System.Collections.Generic;
using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Runtime logic for one stand: an ordered queue of visitor orders and a serve timer. The order at
    /// index 0 is the one being served. Each finished order publishes SaleCompleted with its coin value.
    /// </summary>
    public sealed class StandController
    {
        private const int NoVisitor = -1;

        private readonly EconomyConfig _economy;
        private readonly SaleMultipliers _multipliers;
        private readonly List<Order> _orders;
        private float _serveRemaining;

        public StandController(int index, StandModel model, ProductDefinition product, EconomyConfig economy,
            SaleMultipliers multipliers)
        {
            _multipliers = multipliers;
            Index = index;
            Model = model;
            Product = product;
            _economy = economy;
            _orders = new List<Order>(economy.maxStandQueue);
        }

        public int Index { get; }
        public StandModel Model { get; }
        public ProductDefinition Product { get; }

        public bool IsServing => _orders.Count > 0;
        public int OrderCount => _orders.Count;
        public bool IsFull => _orders.Count >= _economy.maxStandQueue;
        public float ServeProgress => IsServing ? 1f - _serveRemaining / Product.serveSeconds : 0f;
        public int ServingVisitorId => IsServing ? _orders[0].VisitorId : NoVisitor;

        /// <summary>How fast orders are served (1 = normal); the player's character working here raises it.</summary>
        public float ServeSpeed { get; set; } = 1f;

        /// <summary>Orders taken since launch (not saved); playtests use it to measure demand.</summary>
        public int TotalOrders { get; private set; }

        /// <summary>Adds an order to the back of the queue. Returns false when the queue is full.</summary>
        public bool TryEnqueueOrder(int visitorId, float spendMultiplier)
        {
            if (IsFull)
            {
                return false;
            }

            _orders.Add(new Order(visitorId, spendMultiplier));
            TotalOrders++;
            if (_orders.Count == 1)
            {
                _serveRemaining = Product.serveSeconds;
            }

            return true;
        }

        /// <summary>Position in line (0 = being served) or -1 when the visitor is not queued here.</summary>
        public int GetQueueIndex(int visitorId)
        {
            for (int i = 0; i < _orders.Count; i++)
            {
                if (_orders[i].VisitorId == visitorId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Removes a visitor who gave up. If they were being served, the next order starts fresh.</summary>
        public void RemoveOrder(int visitorId)
        {
            int index = GetQueueIndex(visitorId);
            if (index < 0)
            {
                return;
            }

            _orders.RemoveAt(index);
            if (index == 0 && _orders.Count > 0)
            {
                _serveRemaining = Product.serveSeconds;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsServing)
            {
                return;
            }

            _serveRemaining -= deltaTime * ServeSpeed;
            if (_serveRemaining > 0f)
            {
                return;
            }

            Order served = _orders[0];
            _orders.RemoveAt(0);
            _serveRemaining = Product.serveSeconds;

            long amount = EconomyMath.SaleValue(
                Product.price, served.SpendMultiplier, Model.level, _economy.standLevelIncomeBonus);
            amount = _multipliers.Apply(amount, Product);
            EventBus.Publish(new SaleCompleted(Index, amount, served.VisitorId));
        }

        private readonly struct Order
        {
            public readonly int VisitorId;
            public readonly float SpendMultiplier;

            public Order(int visitorId, float spendMultiplier)
            {
                VisitorId = visitorId;
                SpendMultiplier = spendMultiplier;
            }
        }
    }
}
