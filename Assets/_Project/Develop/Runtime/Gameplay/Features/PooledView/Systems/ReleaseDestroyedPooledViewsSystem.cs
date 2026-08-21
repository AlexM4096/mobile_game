using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Destroy;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.PooledView.Systems
{
    public sealed class ReleaseDestroyedPooledViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DestroySelfRequest, PooledView, GameObjectReference>();

        private readonly IViewPool _viewPool;

        public ReleaseDestroyedPooledViewsSystem(World world, IViewPool viewPool) : base(world)
        {
            _viewPool = viewPool;
        }

        public override void Update(in SystemState state)
        {
            using var buffer = new CommandBuffer();

            World.Query(in _description, (Entity entity, ref GameObjectReference reference) =>
            {
                if (reference.GameObject != null && !_viewPool.Release(reference.GameObject))
                {
                    Object.Destroy(reference.GameObject);
                }

                buffer.Remove<GameObjectReference>(entity);
            });

            buffer.Playback(World, false);
        }
    }
}
