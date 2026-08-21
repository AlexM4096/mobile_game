using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using UnityEngine;

namespace _Project.Gameplay.Features.PooledView.Systems
{
    public sealed class DestroyPositionlessPooledViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PooledView>()
                .WithNone<Position, DestroySelfRequest>();

        public DestroyPositionlessPooledViewsSystem(World world) : base(world) { }

        public override void Update(in SystemState state)
        {
            using var buffer = new CommandBuffer();

            World.Query(in _description, (Entity entity, ref PooledView pooledView) =>
            {
                Debug.LogWarning(
                    $"Pooled-view entity {entity} has pool ID {pooledView.PoolId} but no Position; it will be destroyed.");
                buffer.Add<DestroySelfRequest>(entity);
            });

            buffer.Playback(World, false);
        }
    }
}
