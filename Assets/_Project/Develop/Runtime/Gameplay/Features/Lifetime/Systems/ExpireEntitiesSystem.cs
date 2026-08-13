using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.Lifetime.Systems
{
    public sealed class ExpireEntitiesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Lifetime>();

        public ExpireEntitiesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();
            var deltaTime = state.DeltaTime;

            World.Query(in _description, (Entity entity, ref Lifetime lifetime) =>
            {
                if (lifetime.Value == -1f)
                {
                    return;
                }

                lifetime.Value -= deltaTime;
                if (lifetime.Value <= 0f)
                {
                    commandBuffer.Add<DeadTag>(entity);
                }
            });

            commandBuffer.Playback(World, false);
        }
    }
}