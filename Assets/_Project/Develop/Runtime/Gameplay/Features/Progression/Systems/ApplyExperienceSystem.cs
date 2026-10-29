using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.Progression.Systems
{
    public sealed class ApplyExperienceSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<GrantExperienceRequest>();

        private readonly IUpgradeAvailabilityService _availability;
        private readonly List<Entity> _requestEntities = new();
        private readonly List<GrantExperienceRequest> _requests = new();

        public ApplyExperienceSystem(
            World world,
            IUpgradeAvailabilityService availability) : base(world)
        {
            _availability = availability;
        }

        public override void Update(in SystemState state)
        {
            _requestEntities.Clear();
            _requests.Clear();
            World.Query(in _description, (Entity entity, ref GrantExperienceRequest request) =>
            {
                _requestEntities.Add(entity);
                _requests.Add(request);
            });

            for (var index = 0; index < _requests.Count; index++)
            {
                var request = _requests[index];
                if (request.Amount > 0 &&
                    World.IsAlive(request.Target) &&
                    World.Has<CharacterProgress>(request.Target))
                {
                    ref var progress = ref World.Get<CharacterProgress>(request.Target);
                    if (progress.Level < _availability.MaximumRunLevel)
                    {
                        progress.Experience += request.Amount;
                    }
                }

                if (World.IsAlive(_requestEntities[index]))
                {
                    World.Destroy(_requestEntities[index]);
                }
            }
        }
    }
}
