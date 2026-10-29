using Arch.Core;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.Progression.Systems
{
    public sealed class ResolveLevelUpsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<CharacterProgress>();

        private readonly ProgressionConfig _config;
        private readonly IUpgradeAvailabilityService _availability;

        public ResolveLevelUpsSystem(
            World world,
            ProgressionConfig config,
            IUpgradeAvailabilityService availability) : base(world)
        {
            _config = config;
            _availability = availability;
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _description, (ref CharacterProgress progress) =>
            {
                while (progress.Level < _availability.MaximumRunLevel &&
                       progress.Experience >= progress.RequiredExperience)
                {
                    progress.Experience -= progress.RequiredExperience;
                    progress.Level++;
                    progress.PendingSelections++;
                    progress.RequiredExperience =
                        _config.BaseExperience +
                        (progress.Level - 1) * _config.ExperienceIncreasePerLevel;
                }

                if (progress.Level >= _availability.MaximumRunLevel)
                {
                    progress.Experience = progress.RequiredExperience;
                }
            });
        }
    }
}
