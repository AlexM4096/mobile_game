using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Collision.Systems;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Systems;
using _Project.Gameplay.Infrastructure;
using Unity.Mathematics;
using UnityEngine;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Editor.Tests
{
    public sealed class SwordDamageTests
    {
        private World _world;
        private CleanupCollisionEventsSystem _cleanupSystem;
        private DetectCollisionsSystem _detectSystem;
        private ApplyCollisionDamageSystem _collisionDamageSystem;
        private ApplyEnemyHitsSystem _enemyHitsSystem;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _cleanupSystem = new CleanupCollisionEventsSystem(_world);
            _detectSystem = new DetectCollisionsSystem(
                _world,
                MovementSwarmTestLifetimeScope.CreateCollisionMatrix());
            _collisionDamageSystem = new ApplyCollisionDamageSystem(_world);
            _enemyHitsSystem = new ApplyEnemyHitsSystem(_world);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void PlayerWeaponLayerOnlyInteractsWithEnemies()
        {
            var matrix = MovementSwarmTestLifetimeScope.CreateCollisionMatrix();

            Assert.That(
                matrix.CanInteract(CollisionLayer.PlayerWeapon, CollisionLayer.Enemy),
                Is.True);
            Assert.That(
                matrix.CanInteract(CollisionLayer.PlayerWeapon, CollisionLayer.Default),
                Is.False);
            Assert.That(
                matrix.CanInteract(CollisionLayer.PlayerWeapon, CollisionLayer.Player),
                Is.False);
            Assert.That(
                matrix.CanInteract(CollisionLayer.PlayerWeapon, CollisionLayer.Environment),
                Is.False);
            Assert.That(
                matrix.CanInteract(CollisionLayer.PlayerWeapon, CollisionLayer.Projectile),
                Is.False);
            Assert.That(
                matrix.CanInteract(CollisionLayer.PlayerWeapon, CollisionLayer.PlayerWeapon),
                Is.False);
        }

        [Test]
        public void SwordHitsOncePerEntryAndCanHitAgainAfterReentry()
        {
            CreateSword(Vector2.zero, 45f);
            var enemy = CreateEnemy(
                new Vector2(0.75f, 0f),
                new EnemyDefense
                {
                    HelmetType = HelmetType.Wood,
                    HelmetDurability = 2,
                    MaxHelmetDurability = 2
                });

            Tick();
            AssertHelmetDurability(enemy, 1);

            Tick();
            AssertHelmetDurability(enemy, 1);

            ref var enemyPosition = ref _world.Get<Position>(enemy);
            enemyPosition.Value = new Vector2(5f, 0f);
            Tick();
            AssertHelmetDurability(enemy, 1);

            enemyPosition.Value = new Vector2(0.75f, 0f);
            Tick();
            AssertHelmetDurability(enemy, 0);
            Assert.That(_world.Has<DeadTag>(enemy), Is.False);
        }

        [Test]
        public void SimultaneousSwordHitsRemoveWeaponThenEliminateEnemy()
        {
            CreateSword(new Vector2(-0.5f, 0f), 0f);
            CreateSword(new Vector2(0.5f, 0f), 180f);
            var enemy = CreateEnemy(
                Vector2.zero,
                new EnemyDefense { HasWeapon = true });

            Tick();

            Assert.That(_world.Get<EnemyDefense>(enemy).HasWeapon, Is.False);
            Assert.That(_world.Has<DeadTag>(enemy), Is.True);
        }

        private void Tick()
        {
            var state = new SystemState();
            _cleanupSystem.Update(in state);
            _detectSystem.Update(in state);
            _collisionDamageSystem.Update(in state);
            _enemyHitsSystem.Update(in state);
        }

        private void CreateSword(Vector2 position, float rotationDegrees)
        {
            _world.Create(
                new Position { Value = position },
                new RotationComponent
                {
                    Value = quaternion.RotateZ(rotationDegrees * Mathf.Deg2Rad)
                },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.PlayerWeapon
                },
                new BoxColliderComponent { HalfExtents = new Vector2(1f, 0.65f) },
                new TriggerTag(),
                new DamageOnCollision
                {
                    Amount = 25f,
                    Phase = CollisionPhase.Enter
                });
        }

        private Entity CreateEnemy(Vector2 position, EnemyDefense defense)
        {
            return _world.Create(
                new Position { Value = position },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Enemy
                },
                new CircleCollider { Radius = 1f },
                defense);
        }

        private void AssertHelmetDurability(Entity entity, int expected)
        {
            Assert.That(
                _world.Get<EnemyDefense>(entity).HelmetDurability,
                Is.EqualTo(expected));
        }
    }
}