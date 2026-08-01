using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision.Components;
using _Project.Gameplay.Features.Health.Components;
using HealthComponent = _Project.Gameplay.Features.Health.Components.Health;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class CollisionDamageSystem : UnitySystemBase
    {
        private readonly QueryDescription query =
            new QueryDescription()
                .WithAll<DamageOnCollision, CollisionEvents>();

        private readonly Dictionary<Entity, float> damageByTarget = new();

        public CollisionDamageSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            damageByTarget.Clear();

            World.Query(in query, (
                ref DamageOnCollision damageOnCollision,
                ref CollisionEvents collisionEvents
            ) =>
            {
                if (damageOnCollision.Amount <= 0f || collisionEvents.Items == null)
                {
                    return;
                }

                foreach (var collisionEvent in collisionEvents.Items)
                {
                    var target = collisionEvent.Other;
                    if (collisionEvent.Type != damageOnCollision.EventType ||
                        !World.IsAlive(target) ||
                        !target.Has<HealthComponent>())
                    {
                        continue;
                    }

                    damageByTarget.TryGetValue(target, out var damage);
                    damageByTarget[target] = damage + damageOnCollision.Amount;
                }
            });

            using var commandBuffer = new CommandBuffer();

            foreach (var targetDamage in damageByTarget)
            {
                var target = targetDamage.Key;
                if (target.TryGet(out Damage damage))
                {
                    damage.Amount += targetDamage.Value;
                    target.Set(damage);
                }
                else
                {
                    commandBuffer.Add(target, new Damage { Amount = targetDamage.Value });
                }
            }

            commandBuffer.Playback(World, false);
        }
    }
}
