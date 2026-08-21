using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using UnityEngine;

namespace _Project.Gameplay.Features.PooledView.Systems
{
    public sealed class AcquireVisiblePooledViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PooledView, Position>()
                .WithNone<GameObjectReference, DestroySelfRequest>();

        private readonly IViewPool _viewPool;
        private readonly IViewVisibilityService _visibility;

        public AcquireVisiblePooledViewsSystem(
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

            World.Query(in _description, (
                Entity entity,
                ref PooledView pooledView,
                ref Position position) =>
            {
                if (!_viewPool.TryGetReleaseDelay(pooledView.PoolId, out var releaseDelay) ||
                    !_visibility.IsVisible(position.Value))
                {
                    return;
                }

                if (_viewPool.TryGet(pooledView.PoolId, out var view) == false)
                {
                    Debug.LogWarning(
                        $"Pooled view for entity {entity} could not be acquired; the entity will be destroyed.");
                    buffer.Add<DestroySelfRequest>(entity);
                    return;
                }

                pooledView.ReleaseTimer = releaseDelay;
                buffer.Add(entity, new GameObjectReference(view));
            });

            buffer.Playback(World, false);
        }
    }
}
