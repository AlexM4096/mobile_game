using System;
using _Project.Gameplay.Features.Spear;

namespace _Project.Gameplay.Features.Progression
{
    public interface IUpgradeAvailabilityService
    {
        bool IsAvailable(UpgradeId upgrade);
        int MaximumRunLevel { get; }
    }

    public sealed class UpgradeAvailabilityService : IUpgradeAvailabilityService
    {
        private readonly bool _isSpearEnabled;

        public UpgradeAvailabilityService(
            ProgressionConfig progressionConfig,
            SpearConfig spearConfig)
        {
            if (progressionConfig == null)
            {
                throw new ArgumentNullException(nameof(progressionConfig));
            }

            if (spearConfig == null)
            {
                throw new ArgumentNullException(nameof(spearConfig));
            }

            _isSpearEnabled = spearConfig.Enabled;
            var availableRanks = 0;
            for (var index = 0; index < progressionConfig.Cards.Count; index++)
            {
                var card = progressionConfig.Cards[index];
                if (card != null && IsAvailable(card.Id))
                {
                    availableRanks += card.MaximumRank;
                }
            }

            MaximumRunLevel = Math.Min(
                progressionConfig.MaximumLevel,
                1 + availableRanks);
        }

        public int MaximumRunLevel { get; }

        public bool IsAvailable(UpgradeId upgrade) =>
            _isSpearEnabled ||
            (upgrade != UpgradeId.Spear && !UpgradeRules.RequiresSpear(upgrade));
    }
}
