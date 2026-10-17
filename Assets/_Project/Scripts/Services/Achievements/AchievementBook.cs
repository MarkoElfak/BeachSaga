using System;
using System.Collections.Generic;
using System.Globalization;
using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Services
{
    /// <summary>
    /// Progress and claim state of every achievement, kept in the saved model list (matched by id, so
    /// achievements can be added or reordered later). Reporting never allocates.
    /// </summary>
    public sealed class AchievementBook
    {
        private readonly AchievementDefinition[] _definitions;
        private readonly AchievementModel[] _models;

        public AchievementBook(AchievementDefinition[] definitions, List<AchievementModel> saved)
        {
            _definitions = definitions ?? Array.Empty<AchievementDefinition>();
            _models = new AchievementModel[_definitions.Length];
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i] == null) continue;
                _models[i] = Find(saved, _definitions[i].id);
                if (_models[i] != null) continue;
                _models[i] = new AchievementModel { id = _definitions[i].id };
                saved.Add(_models[i]);
            }
        }

        public int Count => _definitions.Length;

        public AchievementDefinition Get(int index) => _definitions[index];
        public long Progress(int index) => _models[index] != null ? _models[index].progress : 0;
        public bool IsComplete(int index) => _models[index] != null && _models[index].progress >= _definitions[index].target;
        public bool IsClaimed(int index) => _models[index] != null && _models[index].claimed;
        public bool CanClaim(int index) => IsComplete(index) && !IsClaimed(index);

        /// <summary>0..1 towards the target.</summary>
        public float Fraction(int index) => _models[index] != null ? Math.Min(1f, (float)Progress(index) / _definitions[index].target) : 0f;

        public int CompletedCount => CountWhere(false);
        public int ClaimableCount => CountWhere(true);

        public int IndexOf(string id)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i] != null && _definitions[i].id == id) return i;
            }

            return -1;
        }

        /// <summary>"Sell 100 Donuts" style text (targets get thousands separators).</summary>
        public string Describe(int index)
        {
            AchievementDefinition definition = _definitions[index];
            string product = definition.product != null ? definition.product.displayName : string.Empty;
            return string.Format(definition.description, definition.target.ToString("N0", CultureInfo.InvariantCulture), product);
        }

        /// <summary>Adds to every counter achievement of this goal (SellProduct also matches the product id).</summary>
        public void Add(AchievementGoal goal, string productId, long amount)
        {
            if (amount <= 0) return;
            for (int i = 0; i < _definitions.Length; i++)
            {
                AchievementDefinition definition = _definitions[i];
                if (!Matches(definition, goal, productId) || definition.IsLevelGoal) continue;
                SetProgress(i, _models[i].progress + amount);
            }
        }

        /// <summary>Raises level achievements of this goal to the value reached (never lowers them).</summary>
        public void ReachLevel(AchievementGoal goal, long value)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                AchievementDefinition definition = _definitions[i];
                if (!Matches(definition, goal, null) || !definition.IsLevelGoal || value <= _models[i].progress) continue;
                SetProgress(i, value);
            }
        }

        /// <summary>Marks a completed, unclaimed achievement as claimed. The caller pays the reward.</summary>
        public bool TryClaim(int index)
        {
            if (index < 0 || index >= Count || !CanClaim(index)) return false;
            _models[index].claimed = true;
            return true;
        }

        private void SetProgress(int index, long value)
        {
            AchievementModel model = _models[index];
            long target = _definitions[index].target;
            if (model.progress >= target) return; // finished goals stop counting
            model.progress = Math.Min(target, value);
            EventBus.Publish(new AchievementProgressed(index));
            if (model.progress >= target) EventBus.Publish(new AchievementCompleted(index));
        }

        private static bool Matches(AchievementDefinition definition, AchievementGoal goal, string productId)
        {
            if (definition == null || definition.goal != goal) return false;
            return goal != AchievementGoal.SellProduct || definition.product == null || definition.product.id == productId;
        }

        private int CountWhere(bool claimableOnly)
        {
            int count = 0;
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (claimableOnly ? CanClaim(i) : IsComplete(i)) count++;
            }

            return count;
        }

        private static AchievementModel Find(List<AchievementModel> saved, string id)
        {
            for (int i = 0; i < saved.Count; i++)
            {
                if (saved[i] != null && saved[i].id == id) return saved[i];
            }

            return null;
        }
    }
}
