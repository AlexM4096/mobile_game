using System.Collections.Generic;
using System.Reflection;
using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.EnemySpawn;
using _Project.Gameplay.Features.EnemySpawn.Systems;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Views;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Movement.Systems;
using UnityEngine;

namespace _Project.Editor.Tests
{
    public sealed class SwarmSpawnGeneratorTests
    {
        private static readonly QueryDescription EnemyDescription =
            new QueryDescription().WithAll<MoveSpeed, EnemyDefense, Direction, Velocity>();

        [Test]
        public void GeneratedPositionsRespectWorldSpaceZoneAndShape()
        {
            var random = new System.Random(12345);
            var circle = Zone(SpawnShape.Circle, new Vector2(3f, -2f), true, 10f, Vector2.one);
            var box = Zone(SpawnShape.Box, new Vector2(-4f, 5f), false, 1f, new Vector2(12f, 8f));

            for (var index = 0; index < 1000; index++)
            {
                var circlePosition = SpawnPositionGenerator.GeneratePosition(random, circle);
                Assert.That(Vector2.Distance(circlePosition, circle.Center), Is.EqualTo(10f).Within(0.0001f));

                var boxPosition = SpawnPositionGenerator.GeneratePosition(random, box) - box.Center;
                Assert.That(Mathf.Abs(boxPosition.x), Is.LessThanOrEqualTo(6.0001f));
                Assert.That(Mathf.Abs(boxPosition.y), Is.LessThanOrEqualTo(4.0001f));
            }
        }

        [Test]
        public void IdenticalSeedsProduceIdenticalPositionAndNoiseSequences()
        {
            var zone = Zone(SpawnShape.Box, Vector2.zero, true, 1f, new Vector2(12f, 8f));
            var first = new System.Random(9876);
            var second = new System.Random(9876);

            for (var index = 0; index < 100; index++)
            {
                Assert.That(
                    SpawnPositionGenerator.GeneratePosition(first, zone),
                    Is.EqualTo(SpawnPositionGenerator.GeneratePosition(second, zone)));
                Assert.That(
                    SpawnPositionGenerator.ApplyNoise(first, 20f, 0.2f),
                    Is.EqualTo(SpawnPositionGenerator.ApplyNoise(second, 20f, 0.2f)));
            }
        }

        [Test]
        public void WaveTimelineSupportsDelayCatchUpAndOverlappingEntries()
        {
            var world = World.Create();
            var factory = new EnemyFactory(world);
            var config = ScriptableObject.CreateInstance<EnemySpawnConfig>();
            var firstEnemy = ScriptableObject.CreateInstance<EnemyConfig>();
            var secondEnemy = ScriptableObject.CreateInstance<EnemyConfig>();
            var equipmentVisuals = ScriptableObject.CreateInstance<EnemyEquipmentVisualConfig>();
            var equipmentSprite = CreateSprite();
            var firstPrefab = new GameObject("First Enemy");
            var secondPrefab = new GameObject("Second Enemy");

            try
            {
                world.Create(new PlayerTag(), new Position { Value = Vector2.zero });
                ConfigureEnemy(firstEnemy, firstPrefab, 10f, 0.5f);
                ConfigureEnemy(secondEnemy, secondPrefab, 30f, 1f);
                ConfigureEquipmentVisuals(equipmentVisuals, equipmentSprite);
                Set(config, "equipmentVisuals", equipmentVisuals);
                var zone = Zone(SpawnShape.Circle, Vector2.zero, true, 5f, Vector2.one);
                Set(config, "waves", new List<EnemyWaveConfig>
                {
                    Wave(1f, 2f, zone, Entry(firstEnemy, 4, 2f)),
                    Wave(0.5f, 1f, zone, Entry(secondEnemy, 2, 1f))
                });

                var system = new SpawnEnemiesSystem(world, config, factory);
                system.Initialize();

                Run(system, 0d);
                Assert.That(EnemyCount(world), Is.Zero);
                Run(system, 1d);
                Assert.That(EnemyCount(world), Is.EqualTo(1));
                Run(system, 4d);
                Assert.That(EnemyCount(world), Is.EqualTo(3));
                Run(system, 8d);
                Assert.That(EnemyCount(world), Is.EqualTo(6));

                new CalculateVelocitySystem(world).Update(default);

                world.Query(in EnemyDescription, (
                    Entity _,
                    ref Direction direction,
                    ref MoveSpeed speed,
                    ref Velocity velocity) =>
                {
                    Assert.That(direction.Value.magnitude, Is.EqualTo(1f).Within(0.0001f));
                    Assert.That(velocity.Value.magnitude, Is.EqualTo(speed.Value).Within(0.0001f));
                });
            }
            finally
            {
                factory.Dispose();
                Object.DestroyImmediate(firstPrefab);
                Object.DestroyImmediate(secondPrefab);
                Object.DestroyImmediate(firstEnemy);
                Object.DestroyImmediate(secondEnemy);
                Object.DestroyImmediate(equipmentVisuals);
                Object.DestroyImmediate(equipmentSprite);
                Object.DestroyImmediate(config);
                world.Dispose();
            }
        }

        [Test]
        public void EmptyConfigurationIsRejected()
        {
            var config = ScriptableObject.CreateInstance<EnemySpawnConfig>();
            try
            {
                Assert.That(EnemySpawnConfigValidator.TryValidate(config, out var error), Is.False);
                Assert.That(error, Does.Contain("at least one wave"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static void Run(SpawnEnemiesSystem system, double time) =>
            system.Update(new SystemState { Time = time, DeltaTime = 0.1f });

        private static int EnemyCount(World world)
        {
            var count = 0;
            world.Query(in EnemyDescription, (Entity _, ref EnemyDefense __) => count++);
            return count;
        }

        private static SpawnZone Zone(SpawnShape shape, Vector2 center, bool edge, float radius, Vector2 size)
        {
            var zone = new SpawnZone();
            Set(zone, "shape", shape);
            Set(zone, "center", center);
            Set(zone, "spawnOnEdge", edge);
            Set(zone, "circleRadius", radius);
            Set(zone, "boxSize", size);
            return zone;
        }

        private static EnemyWaveEntry Entry(EnemyConfig enemy, int count, float interval)
        {
            var entry = new EnemyWaveEntry();
            Set(entry, "enemy", enemy);
            Set(entry, "count", count);
            Set(entry, "spawnInterval", interval);
            return entry;
        }

        private static EnemyWaveConfig Wave(float delay, float duration, SpawnZone zone, params EnemyWaveEntry[] entries)
        {
            var wave = new EnemyWaveConfig();
            Set(wave, "delay", delay);
            Set(wave, "duration", duration);
            Set(wave, "spawnZone", zone);
            Set(wave, "composition", new List<EnemyWaveEntry>(entries));
            return wave;
        }

        private static void ConfigureEnemy(EnemyConfig enemy, GameObject prefab, float speed, float radius)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(prefab.transform, false);
            var bodyRenderer = visual.AddComponent<SpriteRenderer>();
            var helmetSocket = new GameObject("HelmetSocket").transform;
            helmetSocket.SetParent(visual.transform, false);
            var weaponSocket = new GameObject("WeaponSocket").transform;
            weaponSocket.SetParent(visual.transform, false);
            var defenseView = prefab.AddComponent<EnemyDefenseView>();
            Set(defenseView, "bodyRenderer", bodyRenderer);
            Set(defenseView, "helmetSocket", helmetSocket);
            Set(defenseView, "weaponSocket", weaponSocket);

            Set(enemy, "prefab", prefab);
            Set(enemy, "baseSpeed", speed);

            Set(enemy, "colliderRadius", radius);
            Set(enemy, "statNoisePercentage", 0f);
        }

        private static Sprite CreateSprite() => Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        private static void ConfigureEquipmentVisuals(
            EnemyEquipmentVisualConfig visuals,
            Sprite sprite)
        {
            Set(visuals, "staffSprite", sprite);
            Set(visuals, "daggerSprite", sprite);
            Set(visuals, "axeSprite", sprite);
            Set(visuals, "clothHelmetSprite", sprite);
            Set(visuals, "woodHelmetSprite", sprite);
            Set(visuals, "ironHelmetSprite", sprite);
        }

        private static void Set(object target, string fieldName, object value) =>
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
    }
}
