using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using UnityEngine;

namespace _Project.Gameplay.Features.Shooting.Systems
{
    public sealed class ExpireProjectilesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<ProjectileTag, ProjectileLifetime, CollisionEvents>();

        public ExpireProjectilesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();
            var deltaTime = state.DeltaTime;

            World.Query(in _description, (
                Entity entity,
                ref ProjectileLifetime lifetime,
                ref CollisionEvents collisionEvents) =>
            {
                lifetime.Value -= deltaTime;
                if (lifetime.Value > 0f && !HasEnemyHit(collisionEvents))
                {
                    return;
                }

                DestroyView(entity);
                commandBuffer.Destroy(entity);
            });

            commandBuffer.Playback(World, false);
        }

        private bool HasEnemyHit(CollisionEvents collisionEvents)
        {
            if (collisionEvents.Items == null)
            {
                return false;
            }

            foreach (var collisionEvent in collisionEvents.Items)
            {
                if (collisionEvent.Type == CollisionEventType.TriggerEnter &&
                    World.IsAlive(collisionEvent.Other) &&
                    World.Has<CircleCollider>(collisionEvent.Other) &&
                    World.Get<CircleCollider>(collisionEvent.Other).Layer == CollisionLayer.Enemy)
                {
                    return true;
                }
            }

            return false;
        }

        private void DestroyView(Entity entity)
        {
            if (!World.Has<GameObjectReference>(entity))
            {
                return;
            }

            var reference = World.Get<GameObjectReference>(entity);
            if (reference.GameObject != null)
            {
                Object.Destroy(reference.GameObject);
            }
        }
    }
}
