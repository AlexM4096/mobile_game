using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using UnityEngine;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class ApplyHealSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<HealRequest, Health>();

        public ApplyHealSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (
                Entity entity,
                ref HealRequest healRequest,
                ref Health health) =>
            {
                health.Current = Mathf.Clamp(health.Current + healRequest.Amount, 0f, health.Max);
                commandBuffer.Remove<HealRequest>(entity);
            });

            commandBuffer.Playback(World, false);
        }
    }
}
