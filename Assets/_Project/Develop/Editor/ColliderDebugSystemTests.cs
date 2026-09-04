using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Collision.Systems;
using _Project.Gameplay.Features.Common;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;
using Object = UnityEngine.Object;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Editor.Tests
{
    public sealed class ColliderDebugSystemTests
    {
        private World _world;
        private ColliderDebugConfig _config;
        private ColliderDebugSystem _system;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _config = ScriptableObject.CreateInstance<ColliderDebugConfig>();
            _system = new ColliderDebugSystem(_world, _config);
            _system.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _system.Dispose();
            Object.DestroyImmediate(_config);
            _world.Dispose();
        }

        [Test]
        public void CollectsValidCircleWithLayer()
        {
            _world.Create(
                new Position { Value = new Vector2(2f, -3f) },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Enemy
                },
                new CircleCollider { Radius = 1.25f });

            _system.Update(new SystemState());

            Assert.That(_system.Shapes, Has.Count.EqualTo(1));
            var debugShape = _system.Shapes[0];
            Assert.That(debugShape.Layer, Is.EqualTo(CollisionLayer.Enemy));
            Assert.That(debugShape.Shape.Type, Is.EqualTo(CollisionShapeType.Circle));
            Assert.That(debugShape.Shape.Position, Is.EqualTo(new Vector2(2f, -3f)));
            Assert.That(debugShape.Shape.Radius, Is.EqualTo(1.25f));
        }

        [Test]
        public void CollectsRotatedBoxGeometry()
        {
            _world.Create(
                new Position { Value = new Vector2(-1f, 4f) },
                new RotationComponent { Value = quaternion.RotateZ(math.radians(90f)) },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.PlayerWeapon
                },
                new BoxColliderComponent { HalfExtents = new Vector2(2f, 0.5f) });

            _system.Update(new SystemState());

            Assert.That(_system.Shapes, Has.Count.EqualTo(1));
            var shape = _system.Shapes[0].Shape;
            Assert.That(shape.Type, Is.EqualTo(CollisionShapeType.Box));
            Assert.That(shape.Position, Is.EqualTo(new Vector2(-1f, 4f)));
            Assert.That(shape.HalfExtents, Is.EqualTo(new Vector2(2f, 0.5f)));
            Assert.That(shape.AxisX.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(shape.AxisX.y, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(shape.AxisY.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(shape.AxisY.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void OmitsInvalidAmbiguousAndNoneLayerColliders()
        {
            _world.Create(
                new Position(),
                CreateBody(CollisionLayer.Default));
            _world.Create(
                new Position(),
                CreateBody(CollisionLayer.Default),
                new CircleCollider { Radius = 1f },
                new BoxColliderComponent { HalfExtents = Vector2.one });
            _world.Create(
                new Position(),
                CreateBody(CollisionLayer.Default),
                new CircleCollider { Radius = 0f });
            _world.Create(
                new Position(),
                CreateBody(CollisionLayer.None),
                new CircleCollider { Radius = 1f });

            _system.Update(new SystemState());

            Assert.That(_system.Shapes, Is.Empty);
        }

        [Test]
        public void ClearsCollectedGeometryWhenDisabled()
        {
            _world.Create(
                new Position(),
                CreateBody(CollisionLayer.Player),
                new CircleCollider { Radius = 1f });
            _system.Update(new SystemState());
            Assert.That(_system.Shapes, Has.Count.EqualTo(1));

            var serializedConfig = new SerializedObject(_config);
            serializedConfig.FindProperty("enabled").boolValue = false;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            _system.Update(new SystemState());

            Assert.That(_system.Shapes, Is.Empty);
        }

        [Test]
        public void ConfigProvidesAColorForEveryActiveLayer()
        {
            Assert.That(_config.Colors, Has.Count.EqualTo(6));
            Assert.That(_config.GetColor(CollisionLayer.None), Is.EqualTo(Color.clear));

            for (var layer = CollisionLayer.Default;
                 layer <= CollisionLayer.PlayerWeapon;
                 layer++)
            {
                Assert.That(_config.Colors.ContainsKey(layer), Is.True);
            }
        }

        private static CollisionBody CreateBody(CollisionLayer layer)
        {
            return new CollisionBody
            {
                BodyType = ColliderBodyType.Dynamic,
                Layer = layer
            };
        }
    }
}
