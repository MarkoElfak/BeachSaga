using UnityEngine;

namespace BeachTycoon.Core
{
    /// <summary>Wallet balance changed. Delta is negative when coins were spent.</summary>
    public readonly struct CoinsChanged
    {
        public readonly long Coins;
        public readonly long Delta;

        public CoinsChanged(long coins, long delta)
        {
            Coins = coins;
            Delta = delta;
        }
    }

    /// <summary>A lot moved between Empty, Building and Built (read the new state from BuildingManager).</summary>
    public readonly struct LotStateChanged
    {
        public readonly int LotIndex;

        public LotStateChanged(int lotIndex)
        {
            LotIndex = lotIndex;
        }
    }

    /// <summary>Construction finished and the building on a lot now has a new tier (1 = first build).</summary>
    public readonly struct TierUpgraded
    {
        public readonly int LotIndex;
        public readonly int Tier;

        public TierUpgraded(int lotIndex, int tier)
        {
            LotIndex = lotIndex;
            Tier = tier;
        }
    }

    /// <summary>A stand was created for a product on a lot.</summary>
    public readonly struct StandCreated
    {
        public readonly int StandIndex;
        public readonly int LotIndex;

        public StandCreated(int standIndex, int lotIndex)
        {
            StandIndex = standIndex;
            LotIndex = lotIndex;
        }
    }

    /// <summary>A stand finished serving one order; Amount coins should drop as a pickup.</summary>
    public readonly struct SaleCompleted
    {
        public readonly int StandIndex;
        public readonly long Amount;

        /// <summary>Visitor who bought, or -1 for an order with no visitor.</summary>
        public readonly int VisitorId;

        public SaleCompleted(int standIndex, long amount, int visitorId)
        {
            StandIndex = standIndex;
            Amount = amount;
            VisitorId = visitorId;
        }
    }

    /// <summary>A stand's upgrade level went up.</summary>
    public readonly struct StandUpgraded
    {
        public readonly int StandIndex;
        public readonly int Level;

        public StandUpgraded(int standIndex, int level)
        {
            StandIndex = standIndex;
            Level = level;
        }
    }

    /// <summary>The player paid to unlock a product (free products only raise StandCreated).</summary>
    public readonly struct ProductUnlocked
    {
        public readonly string ProductId;

        public ProductUnlocked(string productId)
        {
            ProductId = productId;
        }
    }

    /// <summary>Popularity changed noticeably. Spawn interval = base / (1 + Value).</summary>
    public readonly struct PopularityChanged
    {
        public readonly float Value;

        public PopularityChanged(float value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Show the upgrade panel for this lot's building, or for one stand (level up, hire a manager) when
    /// StandIndex is set.
    /// </summary>
    public readonly struct UpgradePanelRequested
    {
        public readonly int LotIndex;
        public readonly int StandIndex;

        public UpgradePanelRequested(int lotIndex, int standIndex = -1)
        {
            LotIndex = lotIndex;
            StandIndex = standIndex;
        }
    }

    /// <summary>A manager was hired for a stand: its coins now collect themselves.</summary>
    public readonly struct ManagerHired
    {
        public readonly int StandIndex;
        public readonly string ProductId;

        public ManagerHired(int standIndex, string productId)
        {
            StandIndex = standIndex;
            ProductId = productId;
        }
    }

    /// <summary>The player collected a coin pickup at a world position.</summary>
    public readonly struct CoinCollected
    {
        public readonly long Amount;
        public readonly Vector3 WorldPosition;

        public CoinCollected(long amount, Vector3 worldPosition)
        {
            Amount = amount;
            WorldPosition = worldPosition;
        }
    }
}
