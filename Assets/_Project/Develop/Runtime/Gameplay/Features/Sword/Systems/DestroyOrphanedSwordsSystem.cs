using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.Player;

namespace _Project.Gameplay.Features.Sword.Systems
{
    public sealed class DestroyOrphanedSwordsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<SwordTag, SwordOrbit>()
                .WithNone<DestroySelfRequest>();

        public DestroyOrphanedSwordsSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (Entity entity, ref SwordOrbit orbit) =>
            {
                if (World.IsAlive(orbit.Owner) &&
                    World.Has<Position>(orbit.Owner) &&
                    World.Has<PlayerMotion>(orbit.Owner))
                {
                    return;
                }

                commandBuffer.Add(entity, new DestroySelfRequest());
            });

            commandBuffer.Playback(World, false);
        }
    }
}
