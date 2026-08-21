using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.PooledView.Systems
{
    public sealed class ReleaseInvisiblePooledViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PooledView, Position, GameObjectReference>()
                .WithNone<DestroySelfRequest>();

        private readonly IViewPool _viewPool;
        private readonly IViewVisibilityService _visibility;

        public ReleaseInvisiblePooledViewsSystem(
            World world,
            IViewPool viewPool,
            IViewVisibilityService visibility) : base(world)
        {
            _viewPool = viewPool;
            _visibility = visibility;
        }

        public override void Update(in SystemState state)
        {
            using var buffer = new CommandBuffer();
            var deltaTime = state.DeltaTime;

            World.Query(in _description, (
                Entity entity,
                ref PooledView pooledView,
                ref Position position,
                ref GameObjectReference reference) =>
            {
                if (!_viewPool.TryGetReleaseDelay(pooledView.PoolId, out var releaseDelay))
                {
                    return;
                }

                if (reference.GameObject == null)
                {
                    Debug.LogWarning(
                        $"Pooled view for entity {entity} was destroyed externally; the entity will be destroyed.");
                    buffer.Remove<GameObjectReference>(entity);
                    buffer.Add<DestroySelfRequest>(entity);
                    return;
                }

                if (_visibility.IsVisible(position.Value))
                {
                    pooledView.ReleaseTimer = releaseDelay;
                    return;
                }

                pooledView.ReleaseTimer -= deltaTime;
                if (pooledView.ReleaseTimer > 0f)
                {
                    return;
                }

                if (!_viewPool.Release(reference.GameObject))
                {
                    Object.Destroy(reference.GameObject);
                }

                pooledView.ReleaseTimer = releaseDelay;
                buffer.Remove<GameObjectReference>(entity);
            });

            buffer.Playback(World, false);
        }
    }
}
