using System.Collections.Generic;
using Arch.Core;
using NUnit.Framework;
using _Project.Gameplay.Features.Arena;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using UnityEngine;
using Object = UnityEngine.Object;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;

namespace _Project.Editor.Tests
{
    public sealed class ArenaBoundaryFactoryTests
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

        [Test]
        public void CreatesFourStaticEnvironmentWallsAroundConfiguredArena()
        {
            var configuration = ScriptableObject.CreateInstance<ArenaBoundsConfig>();
            var factory = new ArenaBoundaryFactory(_world, configuration);

            try
            {
                factory.Create();

                var walls = new List<Wall>();
                var description = new QueryDescription().WithAll<Position, CollisionBody, BoxColliderComponent>();
                _world.Query(in description, (
                    ref Position position,
                    ref CollisionBody body,
                    ref BoxColliderComponent collider) =>
                {
                    walls.Add(new Wall(position.Value, body, collider.HalfExtents));
                });

                Assert.That(walls, Has.Count.EqualTo(4));
                Assert.That(walls.TrueForAll(wall =>
                    wall.Body.BodyType == ColliderBodyType.Static &&
                    wall.Body.Layer == CollisionLayer.Environment), Is.True);
                Assert.That(walls.Exists(wall =>
                    wall.Position == new Vector2(0f, 60.75f) &&
                    wall.HalfExtents == new Vector2(262f, 1f)), Is.True);
                Assert.That(walls.Exists(wall =>
                    wall.Position == new Vector2(0f, -69.25f) &&
                    wall.HalfExtents == new Vector2(262f, 1f)), Is.True);
                Assert.That(walls.Exists(wall =>
                    wall.Position == new Vector2(-261f, -4.25f) &&
                    wall.HalfExtents == new Vector2(1f, 64f)), Is.True);
                Assert.That(walls.Exists(wall =>
                    wall.Position == new Vector2(261f, -4.25f) &&
                    wall.HalfExtents == new Vector2(1f, 64f)), Is.True);
            }
            finally
            {
                factory.Dispose();
                Object.DestroyImmediate(configuration);
            }
        }

        private readonly struct Wall
        {
            public Wall(Vector2 position, CollisionBody body, Vector2 halfExtents)
            {
                Position = position;
                Body = body;
                HalfExtents = halfExtents;
            }

            public Vector2 Position { get; }
            public CollisionBody Body { get; }
            public Vector2 HalfExtents { get; }
        }
    }
}
