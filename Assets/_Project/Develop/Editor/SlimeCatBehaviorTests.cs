using System.Collections.Generic;
using System.Reflection;
using Arch.Core;
using Arch.Unity.Toolkit;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.Destroy.Systems;
using _Project.Gameplay.Features.EnemySpawn;
using _Project.Gameplay.Features.EnemySpawn.Systems;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Views;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Progression;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Editor.Tests
{
    public sealed class SlimeCatBehaviorTests
    {
        private static readonly QueryDescription AliveEnemyDescription =
            new QueryDescription()
                .WithAll<EnemyDefense, Position, MoveSpeed, ExperienceReward>()
                .WithNone<DeadTag>();

        [Test]
        public void LargeSlimeSplitsOnceIntoMediumThenSmallRing()
        {
            var world = World.Create();
            var factory = new EnemyFactory(world);
            var spawnConfig = ScriptableObject.CreateInstance<EnemySpawnConfig>();
            var equipmentVisuals = ScriptableObject.CreateInstance<EnemyEquipmentVisualConfig>();
            var small = CreateEnemyConfig("Small", 4.5f, 1, 1);
            var medium = CreateEnemyConfig("Medium", 3.25f, 2, 2, small, 2);
            var large = CreateEnemyConfig("Large", 2.25f, 4, 3, medium, 2);

            try
            {
                Set(spawnConfig, "randomSeed", 12345);
                Set(spawnConfig, "equipmentVisuals", equipmentVisuals);
                var recipient = world.Create();
                var largeEntity = factory.Create(
                    large,
                    default,
                    equipmentVisuals,
                    Vector2.zero,
                    new System.Random(1),
                    recipient);
                world.Add<DeadTag>(largeEntity);

                new RequestDestoyDeadEntities(world).Update(new SystemState());
                Assert.That(world.Has<DestroySelfRequest>(largeEntity), Is.False);

                var system = new SplitDeadEnemiesSystem(world, spawnConfig, factory);
                system.Update(new SystemState { Time = 0d });
                Assert.That(GetAliveEnemies(world), Is.Empty);
                Assert.That(world.Has<CollisionBody>(largeEntity), Is.False);
                Assert.That(world.Get<MoveSpeed>(largeEntity).Value, Is.Zero);

                system.Update(new SystemState { Time = 0.24d });
                Assert.That(GetAliveEnemies(world), Is.Empty);

                system.Update(new SystemState { Time = 0.25d });
                Assert.That(world.Has<DestroySelfRequest>(largeEntity), Is.True);

                var mediumEntities = GetAliveEnemies(world);
                Assert.That(mediumEntities, Has.Count.EqualTo(2));
                AssertTier(world, mediumEntities, recipient, 2, 2, true);
                Assert.That(
                    world.Get<Position>(mediumEntities[0]).Value.magnitude,
                    Is.EqualTo(1f).Within(0.0001f));
                Assert.That(
                    (world.Get<Position>(mediumEntities[0]).Value +
                     world.Get<Position>(mediumEntities[1]).Value).magnitude,
                    Is.LessThan(0.0001f));

                system.Update(new SystemState { Time = 0.25d });
                Assert.That(GetAliveEnemies(world), Has.Count.EqualTo(2));

                for (var index = 0; index < mediumEntities.Count; index++)
                {
                    world.Add<DeadTag>(mediumEntities[index]);
                }

                system.Update(new SystemState { Time = 1d });
                Assert.That(GetAliveEnemies(world), Is.Empty);
                system.Update(new SystemState { Time = 1.25d });

                var smallEntities = GetAliveEnemies(world);
                Assert.That(smallEntities, Has.Count.EqualTo(4));
                AssertTier(world, smallEntities, recipient, 1, 1, false);

                var totalCascadeExperience = 0;
                var rewardDescription = new QueryDescription().WithAll<ExperienceReward>();
                world.Query(in rewardDescription, (ref ExperienceReward reward) =>
                {
                    totalCascadeExperience += reward.Amount;
                });
                Assert.That(totalCascadeExperience, Is.EqualTo(11));
            }
            finally
            {
                factory.Dispose();
                DestroyEnemyConfig(small);
                DestroyEnemyConfig(medium);
                DestroyEnemyConfig(large);
                Object.DestroyImmediate(equipmentVisuals);
                Object.DestroyImmediate(spawnConfig);
                world.Dispose();
            }
        }

        [Test]
        public void SplitConfigurationCycleIsRejected()
        {
            var spawnConfig = ScriptableObject.CreateInstance<EnemySpawnConfig>();
            var equipmentVisuals = ScriptableObject.CreateInstance<EnemyEquipmentVisualConfig>();
            var equipmentSprite = CreateSprite();
            var first = CreateEnemyConfig("First", 3f, 1, 1);
            var second = CreateEnemyConfig("Second", 3f, 1, 1);

            try
            {
                ConfigureEquipmentVisuals(equipmentVisuals, equipmentSprite);
                Set(first, "splitChild", second);
                Set(first, "splitCount", 1);
                Set(second, "splitChild", first);
                Set(second, "splitCount", 1);
                Set(spawnConfig, "equipmentVisuals", equipmentVisuals);
                Set(spawnConfig, "waves", new List<EnemyWaveConfig>
                {
                    Wave(Entry(first))
                });

                Assert.That(
                    EnemySpawnConfigValidator.TryValidate(spawnConfig, out var error),
                    Is.False);
                Assert.That(error, Does.Contain("split cycle"));
            }
            finally
            {
                DestroyEnemyConfig(first);
                DestroyEnemyConfig(second);
                Object.DestroyImmediate(equipmentSprite);
                Object.DestroyImmediate(equipmentVisuals);
                Object.DestroyImmediate(spawnConfig);
            }
        }

        private static EnemyConfig CreateEnemyConfig(
            string name,
            float speed,
            int bodyDurability,
            int experienceReward,
            EnemyConfig splitChild = null,
            int splitCount = 0)
        {
            var config = ScriptableObject.CreateInstance<EnemyConfig>();
            config.name = name;
            var prefab = new GameObject($"{name} Prefab");
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

            Set(config, "prefab", prefab);
            Set(config, "baseSpeed", speed);
            Set(config, "colliderRadius", 1f);
            Set(config, "statNoisePercentage", 0.1f);
            Set(config, "bodyDurability", bodyDurability);
            Set(config, "experienceReward", experienceReward);
            Set(config, "splitChild", splitChild);
            Set(config, "splitCount", splitCount);
            Set(config, "splitDelay", 0.25f);
            return config;
        }

        private static void AssertTier(
            World world,
            IReadOnlyList<Entity> entities,
            Entity recipient,
            int bodyDurability,
            int experienceReward,
            bool splits)
        {
            for (var index = 0; index < entities.Count; index++)
            {
                var entity = entities[index];
                Assert.That(world.Get<EnemyDefense>(entity).BodyDurability, Is.EqualTo(bodyDurability));
                Assert.That(world.Get<ExperienceReward>(entity).Amount, Is.EqualTo(experienceReward));
                Assert.That(world.Get<ExperienceReward>(entity).Recipient, Is.EqualTo(recipient));
                Assert.That(world.Get<Direction>(entity).Value, Is.EqualTo(Vector2.right));
                Assert.That(world.Has<SplitOnDeath>(entity), Is.EqualTo(splits));
                Assert.That(world.Get<EnemyDefense>(entity).WeaponType, Is.EqualTo(WeaponType.None));
                Assert.That(world.Get<EnemyDefense>(entity).HelmetType, Is.EqualTo(HelmetType.None));
            }
        }

        private static List<Entity> GetAliveEnemies(World world)
        {
            var entities = new List<Entity>();
            world.Query(in AliveEnemyDescription, (Entity entity, ref EnemyDefense _) =>
            {
                entities.Add(entity);
            });
            return entities;
        }

        private static EnemyWaveEntry Entry(EnemyConfig enemy)
        {
            var entry = new EnemyWaveEntry();
            Set(entry, "enemy", enemy);
            Set(entry, "count", 1);
            return entry;
        }

        private static EnemyWaveConfig Wave(EnemyWaveEntry entry)
        {
            var wave = new EnemyWaveConfig();
            Set(wave, "duration", 1f);
            Set(wave, "spawnZone", new SpawnZone());
            Set(wave, "composition", new List<EnemyWaveEntry> { entry });
            return wave;
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

        private static void DestroyEnemyConfig(EnemyConfig config)
        {
            if (config == null)
            {
                return;
            }

            var prefab = config.Prefab;
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(prefab);
        }

        private static void Set(object target, string fieldName, object value) =>
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(target, value);
    }
}
