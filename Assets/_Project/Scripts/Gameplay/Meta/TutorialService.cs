using System;
using BeachTycoon.Core;
using BeachTycoon.Data;

namespace BeachTycoon.Gameplay
{
    /// <summary>
    /// Three-step first-session tutorial driven purely by gameplay events:
    /// build the tent (lot leaves Empty) -> sell a donut (first coin collected) -> upgrade (stand or tier).
    /// Progress is saved, so it never repeats.
    /// </summary>
    public sealed class TutorialService : IDisposable
    {
        public const int BuildStep = 0;
        public const int SellStep = 1;
        public const int UpgradeStep = 2;

        private readonly TutorialConfig _config;
        private readonly MetaModel _meta;
        private readonly BuildingManager _buildings;

        public TutorialService(TutorialConfig config, MetaModel meta, BuildingManager buildings)
        {
            _config = config;
            _meta = meta;
            _buildings = buildings;

            // Saves from before the tutorial existed already have a tent: skip it for them.
            if (_meta.tutorialStep == BuildStep && buildings.MainTier > 0)
            {
                _meta.tutorialStep = StepCount;
            }

            EventBus.Subscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Subscribe<CoinCollected>(OnCoinCollected);
            EventBus.Subscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Subscribe<TierUpgraded>(OnTierUpgraded);
        }

        public int StepCount => _config.steps != null ? _config.steps.Length : 0;
        public int Step => _meta.tutorialStep;
        public bool IsActive => Step < StepCount;
        public TutorialStepDefinition Current => IsActive ? _config.steps[Step] : default;

        public void Dispose()
        {
            EventBus.Unsubscribe<LotStateChanged>(OnLotStateChanged);
            EventBus.Unsubscribe<CoinCollected>(OnCoinCollected);
            EventBus.Unsubscribe<StandUpgraded>(OnStandUpgraded);
            EventBus.Unsubscribe<TierUpgraded>(OnTierUpgraded);
        }

        private void OnLotStateChanged(LotStateChanged evt)
        {
            bool started = evt.LotIndex == BuildingManager.MainLotIndex
                && _buildings.GetLot(evt.LotIndex).state != LotState.Empty;
            if (started) Complete(BuildStep);
        }

        private void OnCoinCollected(CoinCollected evt) => Complete(SellStep);

        private void OnStandUpgraded(StandUpgraded evt) => Complete(UpgradeStep);

        private void OnTierUpgraded(TierUpgraded evt)
        {
            if (evt.LotIndex == BuildingManager.MainLotIndex && evt.Tier > 1) Complete(UpgradeStep);
        }

        private void Complete(int step)
        {
            if (Step != step)
            {
                return;
            }

            _meta.tutorialStep = step + 1;
            EventBus.Publish(new TutorialStepChanged(_meta.tutorialStep));
        }
    }
}
