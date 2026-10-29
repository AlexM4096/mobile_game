using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Death;

namespace _Project.Gameplay.Features.Progression.Systems
{
    public sealed class QueueEnemyExperienceSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DeadTag, ExperienceReward>()
                .WithNone<ExperienceAwardedTag>();

        private readonly List<Entity> _sources = new();
        private readonly List<ExperienceReward> _rewards = new();

        public QueueEnemyExperienceSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            _sources.Clear();
            _rewards.Clear();
            World.Query(in _description, (Entity entity, ref ExperienceReward reward) =>
            {
                _sources.Add(entity);
                _rewards.Add(reward);
            });

            for (var index = 0; index < _sources.Count; index++)
            {
                var source = _sources[index];
                var reward = _rewards[index];
                if (!World.IsAlive(source))
                {
                    continue;
                }

                World.Add<ExperienceAwardedTag>(source);
                if (reward.Amount > 0 && World.IsAlive(reward.Recipient))
                {
                    World.Create(new GrantExperienceRequest
                    {
                        Target = reward.Recipient,
                        Amount = reward.Amount
                    });
                }
            }
        }
    }
}
