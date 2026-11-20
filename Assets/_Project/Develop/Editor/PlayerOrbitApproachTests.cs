using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Player.Systems;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Tests
{
    public sealed class PlayerOrbitApproachTests
    {
        private World _world;
        private PlayerMovementConfig _settings;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _settings = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            var serializedSettings = new SerializedObject(_settings);
            serializedSettings.FindProperty("tangentialSpeed").floatValue = 10f;
            serializedSettings.FindProperty("orbitApproachSpeed").floatValue = 20f;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void ApproachStopsAtOrbitRadiusThenBeginsOrbit()
        {
            var player = _world.Create(
                new PlayerTag(),
                new Position { Value = new Vector2(-20f, 0f) },
                new Direction { Value = Vector2.right },
                new Velocity(),
                new ManualMovementTag(),
                new PlayerMotion
                {
                    Mode = PlayerMotionMode.ApproachOrbit,
                    OrbitCenter = Vector2.zero,
                    OrbitRadius = 8f,
                    OrbitDirection = 1f
                });
            var system = new SimulatePlayerMotionSystem(_world, _settings);

            system.Update(new SystemState { DeltaTime = 0.25f });
            Assert.That(_world.Get<PlayerMotion>(player).Mode, Is.EqualTo(PlayerMotionMode.ApproachOrbit));
            Assert.That(_world.Get<Position>(player).Value, Is.EqualTo(new Vector2(-15f, 0f)));

            system.Update(new SystemState { DeltaTime = 1f });

            Assert.That(_world.Get<PlayerMotion>(player).Mode, Is.EqualTo(PlayerMotionMode.Orbit));
            Assert.That(_world.Get<Position>(player).Value, Is.EqualTo(new Vector2(-8f, 0f)));
            Assert.That(_world.Get<Velocity>(player).Value.magnitude, Is.EqualTo(10f));
        }

        [Test]
        public void ApproachCurvesIntoOrbitNearTheTrajectory()
        {
            var player = _world.Create(
                new PlayerTag(),
                new Position { Value = new Vector2(-10f, 0f) },
                new Direction { Value = Vector2.right },
                new Velocity(),
                new ManualMovementTag(),
                new PlayerMotion
                {
                    Mode = PlayerMotionMode.ApproachOrbit,
                    OrbitCenter = Vector2.zero,
                    OrbitRadius = 8f,
                    OrbitDirection = 1f
                });
            var system = new SimulatePlayerMotionSystem(_world, _settings);

            system.Update(new SystemState { DeltaTime = 0.05f });

            Assert.That(_world.Get<PlayerMotion>(player).Mode, Is.EqualTo(PlayerMotionMode.ApproachOrbit));
            Assert.That(Mathf.Abs(_world.Get<Position>(player).Value.y), Is.GreaterThan(0.001f));
        }

        [Test]
        public void EnteringOrbitAcceleratesFromExistingFlightSpeed()
        {
            var player = _world.Create(
                new PlayerTag(),
                new Position { Value = new Vector2(8f, 0f) },
                new Direction { Value = Vector2.up },
                new Velocity { Value = Vector2.up * 5f },
                new ManualMovementTag(),
                new PlayerMotion
                {
                    Mode = PlayerMotionMode.Orbit,
                    OrbitCenter = Vector2.zero,
                    OrbitRadius = 8f,
                    OrbitDirection = 1f
                });
            var system = new SimulatePlayerMotionSystem(_world, _settings);

            system.Update(new SystemState { DeltaTime = 0.1f });

            Assert.That(_world.Get<Velocity>(player).Value.magnitude, Is.EqualTo(7.8f).Within(0.001f));
        }
    }
}
