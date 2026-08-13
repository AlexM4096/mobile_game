using Arch.Core;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class DestroyEntitiesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DeadTag>();

        public DestroyEntitiesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.Destroy(in _description);
        }
    }
}
