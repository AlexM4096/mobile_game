using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Collision.Systems;
using _Project.Gameplay.Features.Common;
using Unity.Mathematics;
using UnityEngine;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Editor.Tests
{
    public sealed class CollisionSystemTests
    {
        private static readonly QueryDescription _eventDescription =
            new QueryDescription().WithAll<CollisionEvent>();

        private readonly List<CollisionEvent> _events = new();
        private World _world;
        private CollisionMatrix _matrix;
        private CleanupCollisionEventsSystem _cleanupSystem;
        private DetectCollisionsSystem _detectSystem;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _matrix = new CollisionMatrix();
            _cleanupSystem = new CleanupCollisionEventsSystem(_world);
            _detectSystem = new DetectCollisionsSystem(_world, _matrix);
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
        }

        [Test]
        public void EventsFollowEnterStayExitAndAreCleanedNextTick()
        {
            var first = CreateCircle(Vector2.zero, trigger: true);
            var second = CreateCircle(new Vector2(1.5f, 0f), trigger: true);

            Tick();
            AssertSingleEvent(CollisionPhase.Enter, first, second);

            Tick();
            AssertSingleEvent(CollisionPhase.Stay, first, second);

            ref var secondPosition = ref _world.Get<Position>(second);
            secondPosition.Value = new Vector2(3f, 0f);
            Tick();
            AssertSingleEvent(CollisionPhase.Exit, first, second);
            Assert.That(_events[0].Penetration, Is.Zero);

            Tick();
            Assert.That(ReadEvents(), Is.Empty);
        }

        [Test]
        public void EventUsesStablePairOrderAndWorldSpaceContact()
        {
            CreateCircle(Vector2.zero, trigger: true);
            CreateCircle(new Vector2(1.5f, 0f), trigger: true);

            Tick();

            var collisionEvent = ReadSingleEvent();
            var firstPosition = _world.Get<Position>(collisionEvent.First).Value;
            var secondPosition = _world.Get<Position>(collisionEvent.Second).Value;
            var expectedNormal = (secondPosition - firstPosition).normalized;
            AssertVector(collisionEvent.Normal, expectedNormal);
            AssertVector(collisionEvent.Point, new Vector2(0.75f, 0f));
            Assert.That(collisionEvent.Normal.magnitude, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void DisabledLayerPairDoesNotCreateEventOrResolve()
        {
            var first = CreateCircle(
                Vector2.zero,
                ColliderBodyType.Dynamic,
                CollisionLayer.Default);
            var second = CreateCircle(
                new Vector2(1.5f, 0f),
                ColliderBodyType.Dynamic,
                CollisionLayer.Enemy);
            _matrix.SetInteraction(CollisionLayer.Default, CollisionLayer.Enemy, false);

            Tick();

            Assert.That(ReadEvents(), Is.Empty);
            AssertVector(_world.Get<Position>(first).Value, Vector2.zero);
            AssertVector(_world.Get<Position>(second).Value, new Vector2(1.5f, 0f));
        }

        [Test]
        public void TriggerContactDoesNotResolvePositions()
        {
            var first = CreateCircle(Vector2.zero, trigger: true);
            var second = CreateCircle(new Vector2(1.5f, 0f));

            Tick();

            Assert.That(ReadEvents(), Has.Count.EqualTo(1));
            AssertVector(_world.Get<Position>(first).Value, Vector2.zero);
            AssertVector(_world.Get<Position>(second).Value, new Vector2(1.5f, 0f));
        }

        [Test]
        public void DynamicBodyMovesFullyAwayFromStaticBody()
        {
            var dynamicBody = CreateCircle(Vector2.zero);
            var staticBody = CreateCircle(
                new Vector2(1.5f, 0f),
                ColliderBodyType.Static);

            Tick();

            AssertVector(_world.Get<Position>(dynamicBody).Value, new Vector2(-0.5f, 0f));
            AssertVector(_world.Get<Position>(staticBody).Value, new Vector2(1.5f, 0f));
        }

        [Test]
        public void DynamicBodiesSplitPositionalCorrection()
        {
            var first = CreateCircle(new Vector2(-0.75f, 0f));
            var second = CreateCircle(new Vector2(0.75f, 0f));

            Tick();

            AssertVector(_world.Get<Position>(first).Value, new Vector2(-1f, 0f));
            AssertVector(_world.Get<Position>(second).Value, new Vector2(1f, 0f));
        }

        [Test]
        public void EntityWithBothShapesIsIgnored()
        {
            var invalid = CreateCircle(Vector2.zero);
            _world.Add(invalid, new BoxColliderComponent { HalfExtents = Vector2.one });
            CreateBox(new Vector2(0.5f, 0f));

            Tick();

            Assert.That(ReadEvents(), Is.Empty);
        }

        [Test]
        public void NonPositiveShapeDimensionsAreIgnored()
        {
            _world.Create(
                new Position { Value = Vector2.zero },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Default
                },
                new CircleCollider { Radius = 0f });
            CreateCircle(Vector2.zero);

            Tick();

            Assert.That(ReadEvents(), Is.Empty);
        }

        [Test]
        public void RotationComponentOrientsBoxCollider()
        {
            _world.Create(
                new Position { Value = Vector2.zero },
                new RotationComponent { Value = quaternion.RotateZ(Mathf.PI * 0.5f) },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Default
                },
                new BoxColliderComponent { HalfExtents = new Vector2(1f, 0.25f) },
                new TriggerTag());
            _world.Create(
                new Position { Value = new Vector2(0f, 1.4f) },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Default
                },
                new CircleCollider { Radius = 0.5f },
                new TriggerTag());

            Tick();

            Assert.That(ReadEvents(), Has.Count.EqualTo(1));
            Assert.That(_events[0].Penetration, Is.EqualTo(0.1f).Within(0.0001f));
        }

        private void Tick()
        {
            var state = new SystemState();
            _cleanupSystem.Update(in state);
            _detectSystem.Update(in state);
        }

        private Entity CreateCircle(
            Vector2 position,
            ColliderBodyType bodyType = ColliderBodyType.Dynamic,
            CollisionLayer layer = CollisionLayer.Default,
            bool trigger = false)
        {
            var entity = _world.Create(
                new Position { Value = position },
                new CollisionBody
                {
                    BodyType = bodyType,
                    Layer = layer
                },
                new CircleCollider { Radius = 1f });

            if (trigger)
            {
                _world.Add(entity, new TriggerTag());
            }

            return entity;
        }

        private Entity CreateBox(
            Vector2 position,
            ColliderBodyType bodyType = ColliderBodyType.Dynamic,
            CollisionLayer layer = CollisionLayer.Default)
        {
            return _world.Create(
                new Position { Value = position },
                new CollisionBody
                {
                    BodyType = bodyType,
                    Layer = layer
                },
                new BoxColliderComponent { HalfExtents = Vector2.one });
        }

        private void AssertSingleEvent(CollisionPhase phase, Entity first, Entity second)
        {
            var collisionEvent = ReadSingleEvent();
            Assert.That(collisionEvent.Phase, Is.EqualTo(phase));
            Assert.That(
                collisionEvent.First.Equals(first) && collisionEvent.Second.Equals(second) ||
                collisionEvent.First.Equals(second) && collisionEvent.Second.Equals(first),
                Is.True);
        }

        private CollisionEvent ReadSingleEvent()
        {
            var events = ReadEvents();
            Assert.That(events, Has.Count.EqualTo(1));
            return events[0];
        }

        private List<CollisionEvent> ReadEvents()
        {
            _events.Clear();
            _world.Query(in _eventDescription, (ref CollisionEvent collisionEvent) =>
            {
                _events.Add(collisionEvent);
            });
            return _events;
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        }
    }
}
