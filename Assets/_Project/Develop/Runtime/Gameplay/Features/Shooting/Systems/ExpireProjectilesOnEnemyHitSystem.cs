using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using EntityLifetime = _Project.Gameplay.Features.Lifetime.Lifetime;
namespace _Project.Gameplay.Features.Shooting.Systems
{
    public sealed class ExpireProjectilesOnEnemyHitSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<CollisionEvent>();

        public ExpireProjectilesOnEnemyHitSystem(World world) : base(world) { }

        public override void Update(in SystemState state)
        {
            World.Query(in _description, (ref CollisionEvent collisionEvent) =>
            {
                if (collisionEvent.Phase != CollisionPhase.Enter)
                {
                    return;
                }

                ExpireIfProjectileHitEnemy(collisionEvent.First, collisionEvent.Second);
                ExpireIfProjectileHitEnemy(collisionEvent.Second, collisionEvent.First);
            });
        }

        private void ExpireIfProjectileHitEnemy(Entity projectile, Entity other)
        {
            if (!World.IsAlive(projectile) ||
                !World.IsAlive(other) ||
                !World.Has<ProjectileTag>(projectile) ||
                !World.Has<EntityLifetime>(projectile) ||
                !World.Has<TriggerTag>(projectile) ||
                !World.Has<CollisionBody>(other) ||
                World.Get<CollisionBody>(other).Layer != CollisionLayer.Enemy)
            {
                return;
            }

            ref var lifetime = ref World.Get<EntityLifetime>(projectile);
            lifetime.Value = 0f;
        }
    }
}
