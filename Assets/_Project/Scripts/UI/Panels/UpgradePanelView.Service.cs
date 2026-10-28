using BeachTycoon.Core;
using BeachTycoon.Data;
using BeachTycoon.Gameplay;
using UnityEngine;

namespace BeachTycoon.UI
{
    /// <summary>A service building's help in the upgrade panel: its reach, the stands in it, its effect and the next wedding.</summary>
    public sealed partial class UpgradePanelView
    {
        private void AppendService(StandController building)
        {
            if (!building.Product.IsService || !ServiceLocator.TryGet(out ServiceBuildings services)) return;
            ServiceEffect service = building.Product.service;
            _text.Append("\nReach: ").Append(Mathf.RoundToInt(services.Reach(building))).Append(" m, ")
                .Append(services.StandsInReach(building)).Append(" stands inside");
            float effect = services.Effect(building);
            if (effect <= 0f)
            {
                _text.Append("\nNeeds someone at work to help (best with Care).");
                return;
            }

            int percent = Mathf.RoundToInt(effect * 100f);
            if (service.kind == ServiceKind.Weddings)
            {
                _text.Append("\nWedding guests: +").Append(percent).Append("% coins per sale in reach");
                _text.Append("\nNext wedding in ").Append(Mathf.CeilToInt(services.SecondsToWedding(building))).Append(" s, pays ")
                    .Append(NumberFormatter.Format(services.WeddingPayout(building)));
            }
            else
            {
                _text.Append("\nVisitors in reach wait ").Append(percent).Append("% longer in line");
            }

            TierDefinition grows = service.grownTier > 0 ? _database.GetTier(service.grownTier) : null;
            if (grows != null && !building.Product.IsGrownAt(_buildings.MainTier))
            {
                _text.Append("\nGrows into a ").Append(service.grownName).Append(" with the ").Append(grows.displayName);
            }
        }
    }
}
