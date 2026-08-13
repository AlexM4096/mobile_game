using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using UnityEngine;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class ApplyDamageSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DamageRequest, Health>();

        public ApplyDamageSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (
                Entity entity,
                ref DamageRequest damageRequest,
                ref Health health) =>
            {
                health.Current = Mathf.Clamp(health.Current - damageRequest.Amount, 0f, health.Max);
                commandBuffer.Remove<DamageRequest>(entity);

                if (health.Current <= 0)
                    commandBuffer.Add<DeadTag>(entity);
            });

            commandBuffer.Playback(World, false);
        }
    }
}
