using System.Collections.Generic;

namespace _Project.Gameplay.Features.Progression
{
    public sealed class UpgradeOfferGenerator
    {
        private readonly ProgressionConfig _config;
        private readonly IUpgradeAvailabilityService _availability;
        private readonly System.Random _random;
        private readonly List<UpgradeCardDefinition> _eligible = new();

        public UpgradeOfferGenerator(
            ProgressionConfig config,
            IUpgradeAvailabilityService availability)
        {
            _config = config;
            _availability = availability;
            _random = new System.Random(config.RandomSeed);
        }

        public void Generate(
            in UpgradeRanks ranks,
            bool hasSpear,
            bool hasLightningCharge,
            bool hasRainOfArrows,
            List<UpgradeCardDefinition> destination)
        {
            destination.Clear();
            _eligible.Clear();

            for (var index = 0; index < _config.Cards.Count; index++)
            {
                var card = _config.Cards[index];
                var currentRank = UpgradeRules.GetRank(in ranks, card.Id);
                if (!_availability.IsAvailable(card.Id) ||
                    currentRank >= card.MaximumRank ||
                    (card.Id == UpgradeId.Spear && hasSpear) ||
                    (UpgradeRules.RequiresSpear(card.Id) && !hasSpear) ||
                    (card.Id == UpgradeId.LightningCharge && hasLightningCharge) ||
                    (UpgradeRules.RequiresLightningCharge(card.Id) && !hasLightningCharge) ||
                    (card.Id == UpgradeId.RainOfArrows && hasRainOfArrows) ||
                    (UpgradeRules.RequiresRainOfArrows(card.Id) && !hasRainOfArrows))
                {
                    continue;
                }

                _eligible.Add(card);
            }

            var offerCount = System.Math.Min(_config.OfferedCardCount, _eligible.Count);
            for (var index = 0; index < offerCount; index++)
            {
                var swapIndex = _random.Next(index, _eligible.Count);
                (_eligible[index], _eligible[swapIndex]) =
                    (_eligible[swapIndex], _eligible[index]);
                destination.Add(_eligible[index]);
            }
        }
    }
}
