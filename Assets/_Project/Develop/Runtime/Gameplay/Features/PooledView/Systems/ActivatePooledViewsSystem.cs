using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Destroy;

namespace _Project.Gameplay.Features.PooledView.Systems
{
    public sealed class ActivatePooledViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PooledView, GameObjectReference>()
                .WithNone<DestroySelfRequest>();

        private readonly IViewPool _viewPool;
        private ActivateQuery _query;

        public ActivatePooledViewsSystem(World world, IViewPool viewPool) : base(world)
        {
            _viewPool = viewPool;
            _query = new ActivateQuery { ViewPool = _viewPool };
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<ActivateQuery, GameObjectReference>(in _description, ref _query);
        }

        private struct ActivateQuery : IForEach<GameObjectReference>
        {
            public IViewPool ViewPool;

            public void Update(ref GameObjectReference reference)
            {
                ViewPool.ActivateIfPending(reference.GameObject);
            }
        }
    }
}
