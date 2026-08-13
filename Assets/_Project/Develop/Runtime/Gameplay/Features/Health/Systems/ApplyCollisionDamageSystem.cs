using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class ApplyCollisionDamageSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DamageOnCollision, CollisionEvents>();

        private readonly Dictionary<Entity, float> _damageByTarget = new();

        public ApplyCollisionDamageSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            _damageByTarget.Clear();

            World.Query(in _description, (
                ref DamageOnCollision damageOnCollision,
                ref CollisionEvents collisionEvents) =>
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
                        !target.Has<Health>())
                    {
                        continue;
                    }

                    _damageByTarget.TryGetValue(target, out var damage);
                    _damageByTarget[target] = damage + damageOnCollision.Amount;
                }
            });

            using var commandBuffer = new CommandBuffer();
            foreach (var targetDamage in _damageByTarget)
            {
                var target = targetDamage.Key;
                if (target.TryGet(out DamageRequest damageRequest))
                {
                    damageRequest.Amount += targetDamage.Value;
                    target.Set(damageRequest);
                }
                else
                {
                    commandBuffer.Add(target, new DamageRequest { Amount = targetDamage.Value });
                }
            }

            commandBuffer.Playback(World, false);
        }
    }
}
