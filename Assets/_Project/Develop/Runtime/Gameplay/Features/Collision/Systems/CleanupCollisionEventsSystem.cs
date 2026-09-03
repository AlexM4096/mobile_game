using Arch.Core;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.Collision.Systems
{
    public sealed class CleanupCollisionEventsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<CollisionEvent>();

        public CleanupCollisionEventsSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.Destroy(in _description);
        }
    }
}
