using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using UnityEngine;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class DestroyViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DeadTag, GameObjectReference>();

        public DestroyViewsSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var buffer = new CommandBuffer();

            World.Query(in _description, (Entity entity, ref GameObjectReference reference) =>
            {
                Object.Destroy(reference.GameObject);
                buffer.Remove<GameObjectReference>(entity);
            });

            buffer.Playback(World);
        }
    }
}
