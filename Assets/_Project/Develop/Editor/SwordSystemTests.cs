using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Sword;
using _Project.Gameplay.Features.Sword.Systems;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Editor.Tests
{
    public sealed class SwordSystemTests
    {
        private const float Tolerance = 0.0001f;

        private static readonly QueryDescription _swordDescription =
            new QueryDescription().WithAll<SwordTag, SwordOrbit>();

        private readonly List<Entity> _swords = new();
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

        [Test]
        public void FactoryCreatesConfiguredSwordsAtEvenIntervals()
        {
            var prefab = new GameObject("Sword Test Prefab");
            var viewRoot = new GameObject("Sword Test Views");
            var config = CreateConfig(prefab, weaponCount: 3);

            try
            {
                var owner = _world.Create(
                    new Position { Value = new Vector2(3f, -2f) },
                    new PlayerMotion { Mode = PlayerMotionMode.Flight });

                new SwordFactory(_world, config).CreateFor(owner, viewRoot.transform);
                ReadSwords();

                Assert.That(_swords, Has.Count.EqualTo(3));
                Assert.That(viewRoot.transform.childCount, Is.EqualTo(3));

                _swords.Sort((first, second) =>
                    _world.Get<SwordOrbit>(first).AngleRadians.CompareTo(
                        _world.Get<SwordOrbit>(second).AngleRadians));

                for (var index = 0; index < _swords.Count; index++)
                {
                    var entity = _swords[index];
                    var expectedAngle = Mathf.PI * 2f * index / 3f;
                    var expectedDirection = new Vector2(
                        Mathf.Cos(expectedAngle),
                        Mathf.Sin(expectedAngle));

                    Assert.That(
                        _world.Get<SwordOrbit>(entity).AngleRadians,
                        Is.EqualTo(expectedAngle).Within(Tolerance));
                    AssertVector(
                        _world.Get<Position>(entity).Value,
                        new Vector2(3f, -2f) + expectedDirection * 2f);
                    Assert.That(_world.Has<TriggerTag>(entity), Is.True);
                    Assert.That(
                        _world.Get<CollisionBody>(entity).Layer,
                        Is.EqualTo(CollisionLayer.PlayerWeapon));
                    AssertVector(
                        _world.Get<BoxColliderComponent>(entity).HalfExtents,
                        new Vector2(1f, 0.65f));
                    Assert.That(
                        _world.Get<DamageOnCollision>(entity).Amount,
                        Is.EqualTo(25f).Within(Tolerance));
                    Assert.That(
                        _world.Get<DamageOnCollision>(entity).Phase,
                        Is.EqualTo(CollisionPhase.Enter));
                }
            }
            finally
            {
                Object.DestroyImmediate(viewRoot);
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void OrbitFollowsOwnerMatchesPlayerOrbitAndRestoresConfiguredDirection()
        {
            var config = CreateConfig();

            try
            {
                var owner = _world.Create(
                    new Position { Value = Vector2.zero },
                    new Velocity { Value = Vector2.right * 100f },
                    new PlayerMotion { Mode = PlayerMotionMode.Flight });
                var sword = _world.Create(
                    new SwordTag(),
                    new SwordOrbit { Owner = owner, AngleRadians = 0f },
                    new Position(),
                    new RotationComponent { Value = quaternion.identity });
                var system = new UpdateSwordOrbitSystem(_world, config);
                var state = new SystemState { DeltaTime = 0.5f };

                system.Update(in state);

                Assert.That(
                    _world.Get<SwordOrbit>(sword).AngleRadians,
                    Is.EqualTo(Mathf.PI * 1.5f).Within(Tolerance));
                AssertVector(_world.Get<Position>(sword).Value, new Vector2(0f, -2f));
                AssertPointsOutward(sword, Vector2.down);

                ref var ownerPosition = ref _world.Get<Position>(owner);
                ownerPosition.Value = new Vector2(10f, 5f);
                ref var ownerMotion = ref _world.Get<PlayerMotion>(owner);
                ownerMotion.Mode = PlayerMotionMode.Orbit;
                ownerMotion.OrbitDirection = 1f;

                system.Update(in state);

                Assert.That(
                    _world.Get<SwordOrbit>(sword).AngleRadians,
                    Is.Zero.Within(Tolerance));
                AssertVector(_world.Get<Position>(sword).Value, new Vector2(12f, 5f));
                AssertPointsOutward(sword, Vector2.right);

                ownerMotion.Mode = PlayerMotionMode.Flight;
                system.Update(in state);

                Assert.That(
                    _world.Get<SwordOrbit>(sword).AngleRadians,
                    Is.EqualTo(Mathf.PI * 1.5f).Within(Tolerance));
                AssertVector(_world.Get<Position>(sword).Value, new Vector2(10f, 3f));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void OrphanedSwordRequestsDestruction()
        {
            var owner = _world.Create(
                new Position(),
                new PlayerMotion { Mode = PlayerMotionMode.Flight });
            var sword = _world.Create(
                new SwordTag(),
                new SwordOrbit { Owner = owner });
            _world.Destroy(owner);
            var system = new DestroyOrphanedSwordsSystem(_world);
            var state = new SystemState();

            system.Update(in state);

            Assert.That(_world.Has<DestroySelfRequest>(sword), Is.True);
        }

        private SwordConfig CreateConfig(
            GameObject prefab = null,
            int weaponCount = 1)
        {
            var config = ScriptableObject.CreateInstance<SwordConfig>();
            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("prefab").objectReferenceValue = prefab;
            serializedConfig.FindProperty("rotationSpeedDegreesPerSecond").floatValue = 180f;
            serializedConfig.FindProperty("rotationRadius").floatValue = 2f;
            serializedConfig.FindProperty("direction").enumValueIndex = 0;
            serializedConfig.FindProperty("weaponCount").intValue = weaponCount;
            serializedConfig.FindProperty("damage").floatValue = 25f;
            serializedConfig.FindProperty("hitboxSize").vector2Value = new Vector2(2f, 1.3f);
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        private void ReadSwords()
        {
            _swords.Clear();
            _world.Query(in _swordDescription, (Entity entity, ref SwordOrbit orbit) =>
            {
                _swords.Add(entity);
            });
        }

        private void AssertPointsOutward(Entity sword, Vector2 expectedDirection)
        {
            var rotation = _world.Get<RotationComponent>(sword).Value;
            var worldDirection = math.mul(rotation, new float3(1f, 0f, 0f));
            AssertVector(
                new Vector2(worldDirection.x, worldDirection.y),
                expectedDirection);
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(Tolerance));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(Tolerance));
        }
    }
}
