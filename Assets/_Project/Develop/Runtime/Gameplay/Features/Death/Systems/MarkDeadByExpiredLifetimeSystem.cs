using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using LifetimeC = _Project.Gameplay.Features.Lifetime.Lifetime;

namespace _Project.Gameplay.Features.Death.Systems
{
    public sealed class MarkDeadByExpiredLifetimeSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description = 
            new QueryDescription()
                .WithAll<LifetimeC>()
                .WithNone<DeadTag>();

        public MarkDeadByExpiredLifetimeSystem(World world) : base(world) { }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (Entity entity, ref LifetimeC lifetime) =>
            {
                if (lifetime.Value < 0)
                {
                    commandBuffer.Add<DeadTag>(entity);
                }
            });

            commandBuffer.Playback(World, false);
        }
    }
}