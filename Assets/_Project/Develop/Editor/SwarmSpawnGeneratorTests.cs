using System.Collections.Generic;
using Arch.Core;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace _Project.Editor.Tests
{
    public sealed class SwarmSpawnGeneratorTests
    {
        private static readonly QueryDescription EnemyDescription =
            new QueryDescription()
                .WithAll<MoveSpeed, Health, CircleCollider>();

        [Test]
        public void CircleInsidePositionsStayInsideRadius()
        {
            var random = new System.Random(12345);

            for (var index = 0; index < 1000; index++)
            {
                var position = Generate(random, SpawnShape.Circle, false);

                Assert.That(position.magnitude, Is.LessThanOrEqualTo(10.0001f));
            }
        }

        [Test]
        public void CircleEdgePositionsStayOnRadius()
        {
            var random = new System.Random(12345);

            for (var index = 0; index < 1000; index++)
            {
                var position = Generate(random, SpawnShape.Circle, true);

                Assert.That(position.magnitude, Is.EqualTo(10f).Within(0.0001f));
            }
        }

        [Test]
        public void BoxInsidePositionsStayInsideExtents()
        {
            var random = new System.Random(12345);

            for (var index = 0; index < 1000; index++)
            {
                var position = Generate(random, SpawnShape.Box, false);

                Assert.That(Mathf.Abs(position.x), Is.LessThanOrEqualTo(6.0001f));
                Assert.That(Mathf.Abs(position.y), Is.LessThanOrEqualTo(4.0001f));
            }
        }

        [Test]
        public void BoxEdgePositionsStayOnPerimeterAndCoverEverySide()
        {
            var random = new System.Random(12345);
            var sidesVisited = new bool[4];

            for (var index = 0; index < 1000; index++)
            {
                var position = Generate(random, SpawnShape.Box, true);
                var onTop = Mathf.Approximately(position.y, 4f);
                var onRight = Mathf.Approximately(position.x, 6f);
                var onBottom = Mathf.Approximately(position.y, -4f);
                var onLeft = Mathf.Approximately(position.x, -6f);

                Assert.That(onTop || onRight || onBottom || onLeft, Is.True);
                Assert.That(Mathf.Abs(position.x), Is.LessThanOrEqualTo(6.0001f));
                Assert.That(Mathf.Abs(position.y), Is.LessThanOrEqualTo(4.0001f));

                sidesVisited[0] |= onTop;
                sidesVisited[1] |= onRight;
                sidesVisited[2] |= onBottom;
                sidesVisited[3] |= onLeft;
            }

            Assert.That(sidesVisited, Is.All.True);
        }

        [Test]
        public void IdenticalSeedsProduceIdenticalPositionAndNoiseSequences()
        {
            var first = new System.Random(9876);
            var second = new System.Random(9876);

            for (var index = 0; index < 100; index++)
            {
                Assert.That(
                    Generate(first, SpawnShape.Box, true),
                    Is.EqualTo(Generate(second, SpawnShape.Box, true)));
                Assert.That(
                    SwarmSpawnGenerator.ApplyNoise(first, 20f, 0.2f),
                    Is.EqualTo(SwarmSpawnGenerator.ApplyNoise(second, 20f, 0.2f)));
            }
        }

        [Test]
        public void NoiseRemainsWithinConfiguredPercentage()
        {
            var random = new System.Random(12345);

            for (var index = 0; index < 1000; index++)
            {
                var value = SwarmSpawnGenerator.ApplyNoise(random, 100f, 0.2f);

                Assert.That(value, Is.InRange(80f, 120f));
            }
        }

        [Test]
        public void SpawnerUsesAllProfilesAndAppliesTheirStats()
        {
            var world = World.Create();
            var swarmConfig = ScriptableObject.CreateInstance<SwarmConfig>();
            var playerMovementConfig = ScriptableObject.CreateInstance<PlayerMovementConfig>();
            var playerPrefab = new GameObject("Player Test Prefab");
            var firstEnemyPrefab = new GameObject("First Enemy Test Prefab");
            var secondEnemyPrefab = new GameObject("Second Enemy Test Prefab");

            try
            {
                ConfigureSwarm(
                    swarmConfig,
                    playerPrefab,
                    (firstEnemyPrefab, 2, 10f, 20f, 0.5f),
                    (secondEnemyPrefab, 3, 30f, 40f, 1f));
                LogAssert.Expect(LogType.Error, "Player follow camera requires a main camera.");

                var spawner = new SwarmSpawner(
                    world,
                    swarmConfig,
                    playerMovementConfig,
                    null);
                spawner.Start();

                var enemyCount = 0;
                var firstTypeCount = 0;
                var secondTypeCount = 0;
                world.Query(in EnemyDescription, (Entity entity, ref MoveSpeed speed) =>
                {
                    enemyCount++;
                    var health = world.Get<Health>(entity);
                    var collider = world.Get<CircleCollider>(entity);

                    Assert.That(health.Current, Is.EqualTo(health.Max));
                    if (Mathf.Approximately(collider.Radius, 0.5f))
                    {
                        firstTypeCount++;
                        Assert.That(speed.Value, Is.InRange(8f, 12f));
                        Assert.That(health.Max, Is.InRange(16f, 24f));
                    }
                    else
                    {
                        secondTypeCount++;
                        Assert.That(collider.Radius, Is.EqualTo(1f));
                        Assert.That(speed.Value, Is.InRange(24f, 36f));
                        Assert.That(health.Max, Is.InRange(32f, 48f));
                    }
                });

                Assert.That(enemyCount, Is.EqualTo(5));
                Assert.That(firstTypeCount, Is.EqualTo(2));
                Assert.That(secondTypeCount, Is.EqualTo(3));
            }
            finally
            {
                var spawnedRoot = GameObject.Find("Spawned Enemy Views");
                if (spawnedRoot != null)
                {
                    Object.DestroyImmediate(spawnedRoot);
                }

                Object.DestroyImmediate(playerPrefab);
                Object.DestroyImmediate(firstEnemyPrefab);
                Object.DestroyImmediate(secondEnemyPrefab);
                if (swarmConfig.EnemyRegistry != null)
                {
                    Object.DestroyImmediate(swarmConfig.EnemyRegistry);
                }

                Object.DestroyImmediate(swarmConfig);
                Object.DestroyImmediate(playerMovementConfig);
                world.Dispose();
            }
        }

        private static Vector2 Generate(
            System.Random random,
            SpawnShape shape,
            bool spawnOnEdge)
        {
            return SwarmSpawnGenerator.GeneratePosition(
                random,
                shape,
                spawnOnEdge,
                10f,
                12f,
                8f);
        }

        private static void ConfigureSwarm(
            SwarmConfig config,
            GameObject playerPrefab,
            params (GameObject prefab, int count, float speed, float health, float radius)[] profiles)
        {
            var registry = ScriptableObject.CreateInstance<EnemyRegistryConfig>();
            var serializedRegistry = new SerializedObject(registry);
            var enemies = serializedRegistry.FindProperty("enemies");
            enemies.arraySize = profiles.Length;
            for (var index = 0; index < profiles.Length; index++)
            {
                var profile = profiles[index];
                var element = enemies.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("prefab").objectReferenceValue = profile.prefab;
                element.FindPropertyRelative("enemyCount").intValue = profile.count;
                element.FindPropertyRelative("baseSpeed").floatValue = profile.speed;
                element.FindPropertyRelative("baseHealth").floatValue = profile.health;
                element.FindPropertyRelative("colliderRadius").floatValue = profile.radius;
                element.FindPropertyRelative("statNoisePercentage").floatValue = 0.2f;
            }

            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();

            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("enemyRegistry").objectReferenceValue = registry;
            serializedConfig.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            serializedConfig.FindProperty("spawnShape").enumValueIndex = (int)SpawnShape.Circle;
            serializedConfig.FindProperty("spawnOnEdge").boolValue = false;
            serializedConfig.FindProperty("circleRadius").floatValue = 10f;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
