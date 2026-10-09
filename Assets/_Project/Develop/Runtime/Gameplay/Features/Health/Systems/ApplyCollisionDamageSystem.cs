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
                .WithAll<CollisionEvent>();

        private readonly Dictionary<Entity, float> _damageByTarget = new();
        private readonly Dictionary<Entity, int> _hitsByTarget = new();

        public ApplyCollisionDamageSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            _damageByTarget.Clear();
            _hitsByTarget.Clear();

            World.Query(in _description, (ref CollisionEvent collisionEvent) =>
            {
                AccumulateImpact(collisionEvent.First, collisionEvent.Second, collisionEvent.Phase);
                AccumulateImpact(collisionEvent.Second, collisionEvent.First, collisionEvent.Phase);
            });

            using var commandBuffer = new CommandBuffer();

            foreach (var targetHits in _hitsByTarget)
            {
                var target = targetHits.Key;
                if (target.TryGet(out HitRequest hitRequest))
                {
                    hitRequest.Count += targetHits.Value;
                    target.Set(hitRequest);
                }
                else
                {
                    commandBuffer.Add(target, new HitRequest { Count = targetHits.Value });
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

        private void AccumulateImpact(Entity source, Entity target, CollisionPhase phase)
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
                _hitsByTarget.TryGetValue(target, out var hitCount);
                _hitsByTarget[target] = hitCount + 1;
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