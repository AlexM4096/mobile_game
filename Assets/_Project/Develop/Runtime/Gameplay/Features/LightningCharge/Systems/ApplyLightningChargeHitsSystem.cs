using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Sword;
using UnityEngine;

namespace _Project.Gameplay.Features.LightningCharge.Systems
{
    public sealed class ApplyLightningChargeHitsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<CollisionEvent>();

        private readonly LightningTargetRegistry _targets;
        private readonly LightningArcFactory _arcFactory;
        private readonly List<Impact> _impacts = new();
        private readonly Dictionary<Entity, int> _hitsByEnemy = new();
        private readonly HashSet<Entity> _visitedTargets = new();

        public ApplyLightningChargeHitsSystem(
            World world,
            LightningTargetRegistry targets,
            LightningArcFactory arcFactory) : base(world)
        {
            _targets = targets;
            _arcFactory = arcFactory;
        }

        public override void Update(in SystemState state)
        {
            _impacts.Clear();
            _hitsByEnemy.Clear();

            World.Query(in _description, (ref CollisionEvent collisionEvent) =>
            {
                if (collisionEvent.Phase != CollisionPhase.Enter)
                {
                    return;
                }

                CollectImpact(collisionEvent.First, collisionEvent.Second);
                CollectImpact(collisionEvent.Second, collisionEvent.First);
            });

            _impacts.Sort(ImpactComparer.Instance);
            for (var index = 0; index < _impacts.Count; index++)
            {
                ApplyImpact(_impacts[index], state.Time);
            }

            using var commandBuffer = new CommandBuffer();
            foreach (var enemyHits in _hitsByEnemy)
            {
                var enemy = enemyHits.Key;
                if (!World.IsAlive(enemy) || World.Has<DeadTag>(enemy))
                {
                    continue;
                }

                if (enemy.TryGet(out HitRequest hitRequest))
                {
                    hitRequest.Count += enemyHits.Value;
                    enemy.Set(hitRequest);
                }
                else
                {
                    commandBuffer.Add(enemy, new HitRequest { Count = enemyHits.Value });
                }
            }

            commandBuffer.Playback(World, false);
        }

        private void CollectImpact(Entity sword, Entity enemy)
        {
            if (!World.IsAlive(sword) ||
                !World.Has<SwordTag>(sword) ||
                !World.Has<SwordOrbit>(sword) ||
                !_targets.TryGetPosition(enemy, out _))
            {
                return;
            }

            _impacts.Add(new Impact(sword, enemy));
        }

        private void ApplyImpact(in Impact impact, double currentTime)
        {
            if (!World.IsAlive(impact.Sword) ||
                !World.Has<SwordOrbit>(impact.Sword) ||
                !World.Has<Position>(impact.Sword) ||
                !_targets.TryGetPosition(impact.Enemy, out var currentPosition))
            {
                return;
            }

            ref var orbit = ref World.Get<SwordOrbit>(impact.Sword);
            if (!World.IsAlive(orbit.Owner) ||
                !World.Has<LightningChargeAbility>(orbit.Owner))
            {
                return;
            }

            ref var ability = ref World.Get<LightningChargeAbility>(orbit.Owner);
            if (ability.RemainingCharges <= 0 || currentTime >= ability.ChargeExpiresAt)
            {
                return;
            }

            ability.RemainingCharges--;
            ability.ChargeExpiresAt = ability.RemainingCharges > 0
                ? currentTime + ability.ChargeDuration
                : 0d;

            _visitedTargets.Clear();
            _visitedTargets.Add(impact.Enemy);
            AccumulateHit(impact.Enemy, ability.DamageHits);

            var swordPosition = World.Get<Position>(impact.Sword).Value;
            _arcFactory.Create(swordPosition, currentPosition);

            for (var targetIndex = 1;
                 targetIndex < ability.MaxTargetsPerChain;
                 targetIndex++)
            {
                if (!_targets.TryGetNearest(
                        currentPosition,
                        ability.TransferRadius,
                        _visitedTargets,
                        out var nextEnemy,
                        out var nextPosition))
                {
                    break;
                }

                _visitedTargets.Add(nextEnemy);
                AccumulateHit(nextEnemy, ability.DamageHits);
                _arcFactory.Create(currentPosition, nextPosition);
                currentPosition = nextPosition;
            }
        }

        private void AccumulateHit(Entity enemy, int hitCount)
        {
            _hitsByEnemy.TryGetValue(enemy, out var currentHits);
            _hitsByEnemy[enemy] = currentHits + hitCount;
        }

        private readonly struct Impact
        {
            public Impact(Entity sword, Entity enemy)
            {
                Sword = sword;
                Enemy = enemy;
            }

            public Entity Sword { get; }
            public Entity Enemy { get; }
        }

        private sealed class ImpactComparer : IComparer<Impact>
        {
            public static readonly ImpactComparer Instance = new();

            public int Compare(Impact first, Impact second)
            {
                var swordComparison = first.Sword.CompareTo(second.Sword);
                return swordComparison != 0
                    ? swordComparison
                    : first.Enemy.CompareTo(second.Enemy);
            }
        }
    }
}
