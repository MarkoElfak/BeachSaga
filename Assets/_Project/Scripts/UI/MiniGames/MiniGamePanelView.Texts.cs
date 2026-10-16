using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>The mini-game window's texts: level and reward on the intro, the outcome on the result page.</summary>
    public sealed partial class MiniGamePanelView
    {
        private static readonly string[] Lives = { "No lives left", "1 life", "2 lives", "3 lives", "4 lives", "5 lives" };

        private static string LivesText(int lives) => lives < Lives.Length ? Lives[lives] : lives + " lives";

        public string LevelText => levelInfo.text;
        public string ResultTitleText => resultTitle.text;

        private string ProductName => Definition.product != null ? Definition.product.displayName : "Stand";

        private int Percent(int levelsPassed) =>
            Mathf.RoundToInt((MiniGameService.IncomeMultiplier(Definition, levelsPassed) - 1f) * 100f);

        private void FillIntro()
        {
            int passed = _games.LevelsPassed(Definition);
            int count = Definition.LevelCount;
            int next = _games.NextLevelIndex(Definition);
            bool complete = passed >= count;
            levelInfo.text = complete ? $"All {count} levels passed!" : $"Level {next + 1} of {count}";
            rewardInfo.text = complete
                ? $"{ProductName} sell for +{Percent(passed)}% for good. Replay the last level any time."
                : $"Pass it: {ProductName} sell for <b>+{Percent(passed + 1)}%</b> (now +{Percent(passed)}%) and the stand gets a new decoration.";
            playLabel.text = complete ? $"Replay level {count}" : $"Play level {next + 1}";
        }

        private void FillResult(bool won)
        {
            int passed = _games.LevelsPassed(Definition);
            bool complete = passed >= Definition.LevelCount;
            if (won)
            {
                resultTitle.text = $"Level {LevelIndex + 1} passed!";
                resultBody.text = LastRunRewarded
                    ? $"{ProductName} now sell for +{Percent(passed)}% for good, and the stand got a new decoration."
                    : "Well played! Every level is already passed.";
            }
            else
            {
                bool outOfTime = Game.HasTimeLimit && Game.TimeLeft <= 0f && Game.LivesLeft > 0;
                resultTitle.text = outOfTime ? "Time's up!" : "Out of lives!";
                resultBody.text = $"{Game.Score} of {Game.Target} done. Give it another go.";
            }

            againLabel.text = won && LastRunRewarded && !complete ? $"Next: level {passed + 1}" : won ? "Play again" : "Try again";
        }
    }
}
