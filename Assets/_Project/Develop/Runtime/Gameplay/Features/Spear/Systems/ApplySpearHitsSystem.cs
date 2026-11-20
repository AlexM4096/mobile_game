using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Movement;
using UnityEngine;

namespace _Project.Gameplay.Features.Spear.Systems
{
    public sealed class ApplySpearHitsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<CollisionEvent>();

        private readonly Dictionary<Entity, HashSet<Entity>> _hitEnemiesByProjectile = new();
        private readonly Dictionary<Entity, HitRequest> _hitsByEnemy = new();
        private readonly HashSet<Entity> _depletedProjectiles = new();
        private readonly List<Entity> _staleProjectiles = new();

        public ApplySpearHitsSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            CleanupHitHistory();
            _hitsByEnemy.Clear();
            _depletedProjectiles.Clear();

            World.Query(in _description, (ref CollisionEvent collisionEvent) =>
            {
                if (collisionEvent.Phase != CollisionPhase.Enter)
                {
                    return;
                }

                ProcessImpact(collisionEvent.First, collisionEvent.Second, collisionEvent.Normal);
                ProcessImpact(collisionEvent.Second, collisionEvent.First, -collisionEvent.Normal);
            });

            using var commandBuffer = new CommandBuffer();
            foreach (var enemyHits in _hitsByEnemy)
            {
                var enemy = enemyHits.Key;
                if (!World.IsAlive(enemy))
                {
                    continue;
                }

                if (enemy.TryGet(out HitRequest hitRequest))
                {
                    hitRequest.Add(enemyHits.Value.Count, enemyHits.Value.Direction);
                    enemy.Set(hitRequest);
                }
                else
                {
                    commandBuffer.Add(enemy, enemyHits.Value);
                }
            }

            foreach (var projectile in _depletedProjectiles)
            {
                if (World.IsAlive(projectile) && !World.Has<DeadTag>(projectile))
                {
                    commandBuffer.Add<DeadTag>(projectile);
                }

                _hitEnemiesByProjectile.Remove(projectile);
            }

            commandBuffer.Playback(World, false);
        }

        public override void Dispose()
        {
            _hitEnemiesByProjectile.Clear();
            _hitsByEnemy.Clear();
            _depletedProjectiles.Clear();
            _staleProjectiles.Clear();
        }

        private void ProcessImpact(Entity projectileEntity, Entity enemyEntity, Vector2 contactDirection)
        {
            if (!World.IsAlive(projectileEntity) ||
                !World.IsAlive(enemyEntity) ||
                !World.Has<SpearProjectile>(projectileEntity) ||
                World.Has<DeadTag>(projectileEntity) ||
                !World.Has<EnemyDefense>(enemyEntity) ||
                World.Has<DeadTag>(enemyEntity) ||
                !World.Has<CollisionBody>(enemyEntity) ||
                World.Get<CollisionBody>(enemyEntity).Layer != CollisionLayer.Enemy)
            {
                return;
            }

            ref var projectile = ref World.Get<SpearProjectile>(projectileEntity);
            if (projectile.RemainingDurability <= 0)
            {
                return;
            }

            if (!_hitEnemiesByProjectile.TryGetValue(projectileEntity, out var hitEnemies))
            {
                hitEnemies = new HashSet<Entity>();
                _hitEnemiesByProjectile.Add(projectileEntity, hitEnemies);
            }

            if (!hitEnemies.Add(enemyEntity))
            {
                return;
            }

            var direction = projectileEntity.TryGet(out Velocity velocity) && velocity.Value.sqrMagnitude > 0f
                ? velocity.Value.normalized
                : contactDirection;
            _hitsByEnemy.TryGetValue(enemyEntity, out var hits);
            hits.Add(projectile.DamageHits, direction);
            _hitsByEnemy[enemyEntity] = hits;

            projectile.RemainingDurability--;
            if (projectile.RemainingDurability == 0)
            {
                _depletedProjectiles.Add(projectileEntity);
            }
        }

        private void CleanupHitHistory()
        {
            _staleProjectiles.Clear();
            foreach (var entry in _hitEnemiesByProjectile)
            {
                var projectile = entry.Key;
                if (!World.IsAlive(projectile) ||
                    !World.Has<SpearProjectile>(projectile) ||
                    World.Has<DeadTag>(projectile))
                {
                    _staleProjectiles.Add(projectile);
                }
            }

            for (var index = 0; index < _staleProjectiles.Count; index++)
            {
                _hitEnemiesByProjectile.Remove(_staleProjectiles[index]);
            }
        }
    }
}
