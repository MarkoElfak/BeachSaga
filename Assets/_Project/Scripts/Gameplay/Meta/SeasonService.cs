using System;
using BeachTycoon.Data;
using BeachTycoon.Services;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// New season (prestige). Once the main building reaches EconomyConfig.seasonUnlockTier the player may
    /// start over: coins, the main building and every stand reset, and the season's earnings turn into
    /// seashells that raise all income for good. Neighbours, houses, decorations, trophies and the rest of
    /// the meta progress stay. The reset is written to the save and the game reboots from it.
    /// </summary>
    public sealed class SeasonService
    {
        private readonly EconomyConfig _config;
        private readonly GameSaveData _data;
        private readonly IAnalyticsService _analytics;
        private readonly Action _reboot;

        public SeasonService(EconomyConfig config, GameSaveData data, IAnalyticsService analytics, Action reboot)
        {
            _config = config;
            _data = data;
            _analytics = analytics;
            _reboot = reboot;
        }

        /// <summary>1-based number of the season being played.</summary>
        public int Season => _data.meta.seasonsCompleted + 1;
        public long Seashells => _data.meta.seashells;
        public long SeasonEarned => _data.wallet.seasonEarned;
        public float IncomeMultiplier => SeasonMath.IncomeMultiplier(Seashells, _config.incomeBonusPerSeashell);

        /// <summary>Seashells a new season would pay right now.</summary>
        public long SeashellsToEarn => SeasonMath.Seashells(SeasonEarned, _config.seashellCoinUnit, _config.seashellExponent);
        public float NextIncomeMultiplier => SeasonMath.IncomeMultiplier(Seashells + SeashellsToEarn, _config.incomeBonusPerSeashell);

        public int UnlockTier => _config.seasonUnlockTier;
        public bool IsTierReached => MainTier >= _config.seasonUnlockTier;
        public bool CanStartNewSeason => IsTierReached && SeashellsToEarn > 0;

        private int MainTier => _data.lots.Count > BuildingManager.MainLotIndex ? _data.lots[BuildingManager.MainLotIndex].tier : 0;

        /// <summary>Pays the seashells, resets the island in the save and reboots the game from it.</summary>
        public bool StartNewSeason(long startingCoins)
        {
            if (!CanStartNewSeason) return false;

            long earned = SeashellsToEarn;
            ResetIsland(_data, startingCoins);
            _data.meta.seashells += earned;
            _data.meta.seasonsCompleted++;
            _analytics.LogEvent(AnalyticsEvents.SeasonStarted, "seashells", earned);
            _reboot?.Invoke();
            return true;
        }

        /// <summary>Coins, the main building (an empty lot again, where it stood) and every stand start over.</summary>
        public static void ResetIsland(GameSaveData data, long startingCoins)
        {
            data.wallet.coins = startingCoins;
            data.wallet.seasonEarned = 0;
            data.stands.Clear();
            int main = BuildingManager.MainLotIndex;
            if (data.lots.Count <= main) return;

            LotModel old = data.lots[main];
            data.lots[main] = new LotModel(main) { placed = old.placed, x = old.x, z = old.z, yaw = old.yaw };
        }
    }
}
