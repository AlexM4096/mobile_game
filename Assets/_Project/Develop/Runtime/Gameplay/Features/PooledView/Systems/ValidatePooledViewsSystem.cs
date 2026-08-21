using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using UnityEngine;

namespace _Project.Gameplay.Features.PooledView.Systems
{
    public sealed class ValidatePooledViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PooledView, Position>()
                .WithNone<DestroySelfRequest>();

        private readonly IViewPool _viewPool;

        public ValidatePooledViewsSystem(World world, IViewPool viewPool) : base(world)
        {
            _viewPool = viewPool;
        }

        public override void Update(in SystemState state)
        {
            using var buffer = new CommandBuffer();

            World.Query(in _description, (Entity entity, ref PooledView pooledView) =>
            {
                if (_viewPool.TryGetReleaseDelay(pooledView.PoolId, out _))
                {
                    return;
                }

                Debug.LogWarning(
                    $"Pooled-view entity {entity} has invalid or unknown pool ID {pooledView.PoolId}; it will be destroyed.");
                buffer.Add<DestroySelfRequest>(entity);
            });

            buffer.Playback(World, false);
        }
    }
}
