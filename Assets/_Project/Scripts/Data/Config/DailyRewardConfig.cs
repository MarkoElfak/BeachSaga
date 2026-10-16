using System;
using UnityEngine;

namespace BeachTycoon.Data
{
    /// <summary>One day of the login calendar. Coins scale with the island so late-game rewards still matter.</summary>
    [Serializable]
    public struct DailyRewardDay
    {
        [Tooltip("Coins = this many minutes of the island's current income...")]
        [Min(0f)] public float incomeMinutes;

        [Tooltip("...but never less than this (keeps early rewards meaningful).")]
        [Min(0)] public long minimumCoins;

        [Tooltip("Free 2x income boost added on this day, in minutes (0 = none).")]
        [Min(0f)] public float boostMinutes;
    }

    /// <summary>
    /// Daily login rewards: one claim per local calendar day, a streak that resets after a missed day,
    /// and a cycle that starts over after the last day. The calendar shows up to 7 days.
    /// </summary>
    [CreateAssetMenu(menuName = "Beach Tycoon/Config/Daily Rewards", fileName = "DailyRewardConfig")]
    public sealed class DailyRewardConfig : ScriptableObject
    {
        public DailyRewardDay[] days =
        {
            new DailyRewardDay { incomeMinutes = 5f, minimumCoins = 100 },
            new DailyRewardDay { incomeMinutes = 8f, minimumCoins = 150 },
            new DailyRewardDay { incomeMinutes = 10f, minimumCoins = 200 },
            new DailyRewardDay { incomeMinutes = 15f, minimumCoins = 300, boostMinutes = 5f },
            new DailyRewardDay { incomeMinutes = 20f, minimumCoins = 400 },
            new DailyRewardDay { incomeMinutes = 25f, minimumCoins = 500 },
            new DailyRewardDay { incomeMinutes = 45f, minimumCoins = 1000, boostMinutes = 15f },
        };

        [Tooltip("Coin multiplier for watching a rewarded ad when claiming (boost minutes are not multiplied).")]
        [Min(1f)] public float adMultiplier = 2f;

        [Tooltip("Open the calendar by itself at launch when today's reward is waiting (after the tutorial).")]
        public bool openOnLaunch = true;
    }
}
