using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using HealthC = _Project.Gameplay.Features.Health.Health;

namespace _Project.Gameplay.Features.Death.Systems
{
    public sealed class MarkDeadByZeroHealthSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description = 
            new QueryDescription()
                .WithAll<HealthC>()
                .WithNone<DeadTag>();

        public MarkDeadByZeroHealthSystem(World world) : base(world) { }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (Entity entity, ref HealthC health) =>
            {
                if (health.Current <= 0)
                {
                    commandBuffer.Add<DeadTag>(entity);
                }                
            });

            commandBuffer.Playback(World, false);
        }
    }
}