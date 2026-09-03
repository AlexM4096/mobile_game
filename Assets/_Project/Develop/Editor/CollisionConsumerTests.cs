using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Systems;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Player.Systems;
using _Project.Gameplay.Features.Shooting;
using _Project.Gameplay.Features.Shooting.Systems;
using UnityEngine;
using EntityLifetime = _Project.Gameplay.Features.Lifetime.Lifetime;

namespace _Project.Editor.Tests
{
    public sealed class CollisionConsumerTests
    {
        private World _world;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DamageConsumerHandlesEitherEventBodyOrder(bool sourceIsSecond)
        {
            var source = _world.Create(new DamageOnCollision
            {
                Amount = 2f,
                Phase = CollisionPhase.Enter
            });
            var target = _world.Create(new Health { Current = 10f, Max = 10f });
            CreateEvent(source, target, sourceIsSecond, Vector2.right);
            var system = new ApplyCollisionDamageSystem(_world);
            var state = new SystemState();

            system.Update(in state);

            Assert.That(_world.Has<DamageRequest>(target), Is.True);
            Assert.That(_world.Get<DamageRequest>(target).Amount, Is.EqualTo(2f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProjectileExpiryHandlesEitherEventBodyOrder(bool projectileIsSecond)
        {
            var projectile = _world.Create(
                new ProjectileTag(),
                new TriggerTag(),
                new EntityLifetime { Value = 5f });
            var enemy = _world.Create(new CollisionBody
            {
                BodyType = ColliderBodyType.Dynamic,
                Layer = CollisionLayer.Enemy
            });
            CreateEvent(projectile, enemy, projectileIsSecond, Vector2.right);
            var system = new ExpireProjectilesOnEnemyHitSystem(_world);
            var state = new SystemState();

            system.Update(in state);

            Assert.That(_world.Get<EntityLifetime>(projectile).Value, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerBounceHandlesEitherEventBodyOrder(bool playerIsSecond)
        {
            var player = _world.Create(
                new PlayerTag(),
                new Position { Value = Vector2.zero },
                new Direction { Value = Vector2.right },
                new Velocity { Value = Vector2.right * 3f },
                new PlayerMotion { Mode = PlayerMotionMode.Flight });
            var environment = _world.Create(new CollisionBody
            {
                BodyType = ColliderBodyType.Static,
                Layer = CollisionLayer.Environment
            });
            CreateEvent(
                player,
                environment,
                playerIsSecond,
                playerIsSecond ? Vector2.left : Vector2.right);
            var settings = ScriptableObject.CreateInstance<PlayerMovementConfig>();

            try
            {
                var system = new BouncePlayerOnEnvironmentSystem(_world, settings);
                var state = new SystemState();
                system.Update(in state);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }

            Assert.That(_world.Get<Direction>(player).Value.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(_world.Get<Velocity>(player).Value.x, Is.EqualTo(-4f).Within(0.0001f));
        }

        private void CreateEvent(
            Entity primary,
            Entity other,
            bool primaryIsSecond,
            Vector2 normal)
        {
            _world.Create(new CollisionEvent
            {
                First = primaryIsSecond ? other : primary,
                Second = primaryIsSecond ? primary : other,
                Phase = CollisionPhase.Enter,
                Point = Vector2.zero,
                Normal = normal,
                Penetration = 0.5f
            });
        }
    }
}
