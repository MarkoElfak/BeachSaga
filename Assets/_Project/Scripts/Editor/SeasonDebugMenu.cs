using BeachTycoon.Core;
using BeachTycoon.Services;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Play mode shortcut for trying a new season: earned coins count toward the season's seashells.</summary>
    internal static class SeasonDebugMenu
    {
        private const string Item = "Beach Tycoon/Debug/Earn 1M Coins";

        [MenuItem(Item, priority = 90)]
        private static void EarnMillion() => ServiceLocator.Get<IEconomyService>().Earn(1000000);

        [MenuItem(Item, true)]
        private static bool IsPlaying() => Application.isPlaying && ServiceLocator.TryGet(out IEconomyService _);
    }
}
