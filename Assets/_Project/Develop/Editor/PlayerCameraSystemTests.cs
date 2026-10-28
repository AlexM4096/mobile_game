using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Player.Systems;
using UnityEditor;
using UnityEngine;

namespace _Project.Editor.Tests
{
    public sealed class PlayerCameraSystemTests
    {
        private World _world;
        private PlayerCameraConfig _settings;
        private GameObject _cameraTarget;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _settings = ScriptableObject.CreateInstance<PlayerCameraConfig>();
            _cameraTarget = new GameObject("Test Player Camera Target");
        }

        [TearDown]
        public void TearDown()
        {
            _world.Dispose();
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_cameraTarget);
        }

        [Test]
        public void CameraTargetFollowsPlayerDuringFlightAndAfterLaunch()
        {
            var player = CreatePlayer(
                new Vector2(3f, 4f),
                new PlayerMotion
                {
                    Mode = PlayerMotionMode.Flight,
                    OrbitCenter = new Vector2(-8f, 2f)
                });
            var system = new UpdatePlayerCameraTargetSystem(_world, _settings);
            var state = new SystemState();

            system.Update(in state);
            Assert.That((Vector2)_cameraTarget.transform.position, Is.EqualTo(new Vector2(3f, 4f)));

            ref var position = ref _world.Get<Position>(player);
            ref var motion = ref _world.Get<PlayerMotion>(player);
            position.Value = new Vector2(7f, -2f);
            motion.Mode = PlayerMotionMode.Orbit;
            system.Update(in state);
            Assert.That((Vector2)_cameraTarget.transform.position, Is.EqualTo(motion.OrbitCenter));

            motion.Mode = PlayerMotionMode.Flight;
            system.Update(in state);
            Assert.That((Vector2)_cameraTarget.transform.position, Is.EqualTo(position.Value));
        }

        [Test]
        public void CameraTargetStaysAtOrbitCenterWhilePlayerMoves()
        {
            var player = CreatePlayer(
                new Vector2(3f, 4f),
                new PlayerMotion
                {
                    Mode = PlayerMotionMode.Orbit,
                    OrbitCenter = new Vector2(-2f, 6f)
                });
            var system = new UpdatePlayerCameraTargetSystem(_world, _settings);
            var state = new SystemState();

            system.Update(in state);
            ref var position = ref _world.Get<Position>(player);
            position.Value = new Vector2(8f, 9f);
            system.Update(in state);

            Assert.That((Vector2)_cameraTarget.transform.position, Is.EqualTo(new Vector2(-2f, 6f)));
        }

        [Test]
        public void DisabledOrbitLockKeepsFollowingPlayer()
        {
            var serializedSettings = new SerializedObject(_settings);
            serializedSettings.FindProperty("lockToOrbitCenter").boolValue = false;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            CreatePlayer(
                new Vector2(5f, -3f),
                new PlayerMotion
                {
                    Mode = PlayerMotionMode.Orbit,
                    OrbitCenter = new Vector2(-9f, 1f)
                });
            var system = new UpdatePlayerCameraTargetSystem(_world, _settings);
            var state = new SystemState();

            system.Update(in state);

            Assert.That((Vector2)_cameraTarget.transform.position, Is.EqualTo(new Vector2(5f, -3f)));
        }

        private Entity CreatePlayer(Vector2 position, PlayerMotion motion)
        {
            return _world.Create(
                new PlayerTag(),
                new Position { Value = position },
                motion,
                new PlayerCameraTargetReference { Transform = _cameraTarget.transform });
        }
    }
}
