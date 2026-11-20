using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using UnityEngine;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class ApplyCollisionDamageSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<CollisionEvent>();

        private readonly Dictionary<Entity, float> _damageByTarget = new();
        private readonly Dictionary<Entity, HitRequest> _hitsByTarget = new();

        public ApplyCollisionDamageSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            _damageByTarget.Clear();
            _hitsByTarget.Clear();

            World.Query(in _description, (ref CollisionEvent collisionEvent) =>
            {
                AccumulateImpact(collisionEvent.First, collisionEvent.Second, collisionEvent.Phase, collisionEvent.Normal);
                AccumulateImpact(collisionEvent.Second, collisionEvent.First, collisionEvent.Phase, -collisionEvent.Normal);
            });

            using var commandBuffer = new CommandBuffer();

            foreach (var targetHits in _hitsByTarget)
            {
                var target = targetHits.Key;
                if (target.TryGet(out HitRequest hitRequest))
                {
                    hitRequest.Add(targetHits.Value.Count, targetHits.Value.Direction);
                    target.Set(hitRequest);
                }
                else
                {
                    commandBuffer.Add(target, targetHits.Value);
                }
            }

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

        private void AccumulateImpact(Entity source, Entity target, CollisionPhase phase, Vector2 direction)
        {
            if (!World.IsAlive(source) ||
                !World.IsAlive(target) ||
                !source.Has<DamageOnCollision>())
            {
                return;
            }

            ref var damageOnCollision = ref World.Get<DamageOnCollision>(source);
            if (damageOnCollision.Amount <= 0f || damageOnCollision.Phase != phase)
            {
                return;
            }

            if (target.Has<EnemyDefense>())
            {
                _hitsByTarget.TryGetValue(target, out var hits);
                hits.Add(1, direction);
                _hitsByTarget[target] = hits;
                return;
            }

            if (!target.Has<Health>())
            {
                return;
            }

            _damageByTarget.TryGetValue(target, out var damage);
            _damageByTarget[target] = damage + damageOnCollision.Amount;
        }
    }
}
