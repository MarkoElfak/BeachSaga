using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;

namespace BeachTycoon.UI
{
    /// <summary>The upgrade panel's texts: a lot's next building, or a stand's level and who works there.</summary>
    public sealed partial class UpgradePanelView
    {
        private void RefreshLot()
        {
            LotModel lot = _buildings.GetLot(_lotIndex);
            TierDefinition current = _database.GetTier(lot.tier);
            TierDefinition next = _buildings.GetNextTier(_lotIndex);
            string currentName = current != null ? current.displayName : "Empty lot";

            if (lot.state == LotState.Building)
            {
                title.text = currentName;
                _text.Append("Construction in progress...");
                SetButton("Building", false);
            }
            else if (next == null)
            {
                title.text = currentName;
                _text.Append("This is the best building available for now.");
                SetButton("Max", false);
            }
            else
            {
                title.text = $"{currentName} to {next.displayName}";
                _text.Append("Visitors at once: ").Append(next.maxVisitors);
                if (current != null) _text.Append(" (now ").Append(current.maxVisitors).Append(')');
                _text.Append("\nPopularity bonus: +").Append(next.popularityBonus.ToString("0.##"));
                int tier = _buildings.GetLot(_lotIndex).tier;
                if (_lotIndex == BuildingManager.MainLotIndex)
                {
                    _text.Append("\nStaff: ").Append(_staff.Config.SlotsFor(tier + 1)).Append(" (now ").Append(_staff.Slots).Append(')');
                }
                AppendUnlocks(next);
                SetButton("Upgrade  " + NumberFormatter.Format(next.upgradeCost), _economy.CanAfford(next.upgradeCost));
            }
        }

        private void RefreshStand(StandController stand)
        {
            StandModel model = stand.Model;
            EconomyConfig economy = _database.economy;
            title.text = stand.Product.displayName + " stand";
            _text.Append("Level ").Append(model.level + 1).Append(" of ").Append(economy.maxStandLevel + 1);
            _text.Append("\nIncome: +").Append(UnityEngine.Mathf.RoundToInt(model.level * economy.standLevelIncomeBonus * 100f))
                .Append("% (next level +").Append(UnityEngine.Mathf.RoundToInt(economy.standLevelIncomeBonus * 100f)).Append("%)");
            AppendStaff(stand);
            AppendMiniGame(stand.Product);

            if (_stands.IsMaxLevel(stand)) SetButton("Max level", false);
            else
            {
                long cost = _stands.GetUpgradeCost(stand);
                SetButton("Level up  " + NumberFormatter.Format(cost), _economy.CanAfford(cost));
            }

            StaffMember worker = _staff.WorkerAt(stand.Index);
            hireLabel.text = worker != null ? $"About {worker.Model.name}" : "Staff";
            hireButton.interactable = true;
        }

        // Who runs the stand and how fast it serves; level 3 workers also collect the coins.
        private void AppendStaff(StandController stand)
        {
            int speed = UnityEngine.Mathf.RoundToInt(stand.ServeSpeed * 100f);
            StaffMember worker = _staff.WorkerAt(stand.Index);
            if (_characters.HasJoined && _characters.StandIndex == stand.Index)
            {
                _text.Append('\n').Append(_characters.Character.displayName).Append(" works here");
            }
            else if (worker != null)
            {
                WorkerModel model = worker.Model;
                _text.Append('\n').Append(model.name).Append(" works here (level ").Append(model.level).Append(')');
                _text.Append(_staff.CollectsCoins(model) ? ", collecting the coins" : $". From level {_staff.Config.collectCoinsLevel} they collect the coins");
            }
            else
            {
                _text.Append("\nNobody works here. Hire a job seeker from the beach.");
            }

            _text.Append("\nServing speed: ").Append(speed).Append('%');
        }

        // Mini-game levels passed and the sales bonus they give (play it from the stand's Play button).
        private void AppendMiniGame(ProductDefinition product)
        {
            if (!ServiceLocator.TryGet(out MiniGameService games)) return;
            MiniGameDefinition game = games.ForProduct(product.id);
            if (game == null) return;
            int passed = games.LevelsPassed(game);
            _text.Append("\nMini-game: ").Append(passed).Append(" of ").Append(game.LevelCount).Append(" levels, sales +")
                .Append(UnityEngine.Mathf.RoundToInt((games.IncomeMultiplier(product) - 1f) * 100f)).Append('%');
        }

        private void AppendUnlocks(TierDefinition tier)
        {
            if (tier.unlockedProducts == null || tier.unlockedProducts.Length == 0) return;
            _text.Append("\nUnlocks: ");
            for (int i = 0; i < tier.unlockedProducts.Length; i++)
            {
                if (i > 0) _text.Append(", ");
                _text.Append(tier.unlockedProducts[i] != null ? tier.unlockedProducts[i].displayName : "?");
            }
        }
    }
}
