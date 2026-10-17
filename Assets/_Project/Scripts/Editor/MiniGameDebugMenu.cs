using BeachTycoon.UI;
using UnityEditor;

namespace BeachTycoon.EditorTools
{
    /// <summary>Lets mini-games play themselves (good moves at a human pace) to watch or check a level quickly.</summary>
    internal static class MiniGameDebugMenu
    {
        private const string Item = "Beach Tycoon/Debug/Mini-game Autopilot";

        [MenuItem(Item, priority = 95)]
        private static void Toggle() => MiniGamePanelView.Autopilot = !MiniGamePanelView.Autopilot;

        [MenuItem(Item, true)]
        private static bool Validate()
        {
            Menu.SetChecked(Item, MiniGamePanelView.Autopilot);
            return true;
        }
    }
}
