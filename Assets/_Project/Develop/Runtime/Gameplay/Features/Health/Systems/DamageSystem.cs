using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Health.Components;
using UnityEngine;
using HealthComponent = _Project.Gameplay.Features.Health.Components.Health;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class DamageSystem : UnitySystemBase
    {
        private readonly QueryDescription query =
            new QueryDescription()
                .WithAll<Damage>();

        public DamageSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in query, (
                Entity entity,
                ref Damage damage
            ) =>
            {
                commandBuffer.Remove<Damage>(entity);

                if (damage.Amount <= 0f || !entity.TryGet(out HealthComponent health))
                {
                    return;
                }

                health.Current = Mathf.Clamp(health.Current - damage.Amount, 0f, health.Max);
                entity.Set(health);

                if (health.Current <= 0f && entity.Has<Dead>() == false)
                {
                    commandBuffer.Add(entity, new Dead());
                }
            });

            commandBuffer.Playback(World, false);
        }
    }
}
