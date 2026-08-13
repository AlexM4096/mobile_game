using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using EntityLifetime = _Project.Gameplay.Features.Lifetime.Lifetime;
namespace _Project.Gameplay.Features.Shooting.Systems
{
    public sealed class ExpireProjectilesOnEnemyHitSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description = new QueryDescription().WithAll<ProjectileTag, EntityLifetime, CollisionEvents>();
        public ExpireProjectilesOnEnemyHitSystem(World world) : base(world) { }
        public override void Update(in SystemState state)
        {
            World.Query(in _description, (ref EntityLifetime lifetime, ref CollisionEvents collisionEvents) =>
            {
                if (HasEnemyHit(collisionEvents)) lifetime.Value = 0f;
            });
        }
        private bool HasEnemyHit(CollisionEvents collisionEvents)
        {
            if (collisionEvents.Items == null) return false;
            foreach (var collisionEvent in collisionEvents.Items)
            {
                if (collisionEvent.Type == CollisionEventType.TriggerEnter && World.IsAlive(collisionEvent.Other) && World.Has<CircleCollider>(collisionEvent.Other) && World.Get<CircleCollider>(collisionEvent.Other).Layer == CollisionLayer.Enemy) return true;
            }
            return false;
        }
    }
}