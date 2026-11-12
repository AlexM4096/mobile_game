using System;
using Arch.Core;
using Arch.Unity.Toolkit;
using Arch.Unity.Conversion;
using NUnit.Framework;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.RainOfArrows;
using _Project.Gameplay.Features.RainOfArrows.Systems;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Editor.Tests
{
    public sealed class RainOfArrowsTests
    {
        private const string ConfigPath =
            "Assets/_Project/AddressablesResources/RainOfArrows/Configs/RainOfArrowsConfig.asset";

        private World _world;
        private GameObject _gridObject;
        private Grid _grid;
        private RainOfArrowsConfig _config;
        private RainOfArrowsTargetRegistry _targets;
        private RainOfArrowsFactory _factory;

        [SetUp]
        public void SetUp()
        {
            _world = World.Create();
            _gridObject = new GameObject("Rain Of Arrows Test Grid");
            _grid = _gridObject.AddComponent<Grid>();
            _grid.cellSize = new Vector3(2f, 2f, 1f);
            _config = AssetDatabase.LoadAssetAtPath<RainOfArrowsConfig>(ConfigPath);
            Assert.That(_config, Is.Not.Null);
            _targets = new RainOfArrowsTargetRegistry();
            _factory = new RainOfArrowsFactory(_world, _config, _grid);
        }

        [TearDown]
        public void TearDown()
        {
            _factory.Dispose();
            _world.Dispose();
            Object.DestroyImmediate(_gridObject);
        }

        [Test]
        public void ConfigIsValidAndScalesTileValuesBySmallerGridDimension()
        {
            _grid.cellSize = new Vector3(3f, 2f, 1f);

            Assert.That(_config.TryValidate(_grid, out var error), Is.True, error);
            Assert.That(_config.GetTargetingRange(_grid), Is.EqualTo(12f));
            Assert.That(_config.GetZoneRadius(_grid), Is.EqualTo(6f));
            Assert.That(_config.GetImpactRadius(_grid), Is.EqualTo(1f));
            Assert.That(_config.GetFallHeight(_grid), Is.EqualTo(4f));
        }

        [Test]
        public void TargetingChoosesDensestReachableClusterAndUsesDeterministicTieBreaks()
        {
            var first = _world.Create();
            var second = _world.Create();
            var third = _world.Create();
            var outside = _world.Create();
            _targets.Add(first, new Vector2(4f, 0f));
            _targets.Add(second, new Vector2(4.5f, 0f));
            _targets.Add(third, new Vector2(-2f, 0f));
            _targets.Add(outside, new Vector2(20f, 0f));

            Assert.That(
                _targets.TryFindBestZoneCenter(Vector2.zero, 12f, 2f, out var center),
                Is.True);
            Assert.That(center, Is.EqualTo(new Vector2(4f, 0f)));

            _targets.Clear();
            _targets.Add(first, new Vector2(2f, 0f));
            _targets.Add(second, new Vector2(-2f, 0f));
            Assert.That(
                _targets.TryFindBestZoneCenter(Vector2.zero, 12f, 0.5f, out center),
                Is.True);
            Assert.That(center, Is.EqualTo(new Vector2(-2f, 0f)));
        }

        [Test]
        public void UniformSamplerNeverLeavesBombardmentZone()
        {
            var random = new System.Random(12345);
            var center = new Vector2(3f, -2f);
            const float radius = 6f;
            for (var index = 0; index < 1000; index++)
            {
                var sample = LaunchRainOfArrowsProjectilesSystem.SampleImpactPoint(
                    random,
                    center,
                    radius);
                Assert.That((sample - center).sqrMagnitude, Is.LessThanOrEqualTo(radius * radius));
            }
        }

        [Test]
        public void ActivationWaitsForTargetThenStartsCooldownAndCreatesOneZone()
        {
            var player = _world.Create(
                new PlayerTag(),
                new Position { Value = Vector2.zero },
                CreateAbility(nextActivationTime: 10d));
            var system = new ActivateRainOfArrowsSystem(
                _world,
                _config,
                _grid,
                _targets,
                _factory);

            system.Update(new SystemState { Time = 10d });
            Assert.That(_world.Get<RainOfArrowsAbility>(player).NextActivationTime, Is.EqualTo(10d));
            Assert.That(Count<RainOfArrowsZone>(), Is.Zero);

            var enemy = _world.Create();
            _targets.Add(enemy, new Vector2(4f, 0f));
            system.Update(new SystemState { Time = 10d });

            var ability = _world.Get<RainOfArrowsAbility>(player);
            Assert.That(ability.ActiveZones, Is.EqualTo(1));
            Assert.That(ability.NextActivationTime, Is.EqualTo(18d));
            Assert.That(Count<RainOfArrowsZone>(), Is.EqualTo(1));

            system.Update(new SystemState { Time = 20d });
            Assert.That(Count<RainOfArrowsZone>(), Is.EqualTo(1));
        }

        [Test]
        public void SchedulerLaunchesEveryProjectileAndPlacesFinalImpactAtZoneEnd()
        {
            var owner = _world.Create(CreateAbility());
            var ability = _world.Get<RainOfArrowsAbility>(owner);
            var zone = _factory.CreateZone(owner, Vector2.zero, in ability, 0d);
            var system = new LaunchRainOfArrowsProjectilesSystem(_world, _factory);

            system.Update(new SystemState { Time = 0d });
            Assert.That(Count<RainOfArrowsProjectile>(), Is.EqualTo(1));

            var fallTime = _config.GetFallHeight(_grid) / _config.ProjectileSpeed;
            system.Update(new SystemState { Time = _config.Duration - fallTime });
            Assert.That(Count<RainOfArrowsProjectile>(), Is.EqualTo(12));
            Assert.That(
                _world.Get<RainOfArrowsZone>(zone).LaunchedProjectiles,
                Is.EqualTo(12));
            var latestImpactTime = 0d;
            var projectileDescription =
                new QueryDescription().WithAll<RainOfArrowsProjectile>();
            _world.Query(in projectileDescription, (ref RainOfArrowsProjectile projectile) =>
            {
                latestImpactTime = Math.Max(latestImpactTime, projectile.ImpactTime);
            });
            Assert.That(latestImpactTime, Is.EqualTo(4d).Within(0.000001d));
        }

        [Test]
        public void ImpactsUseCurrentRegistryPositionsStackHitsAndIgnoreDeadEnemies()
        {
            var enemy = CreateEnemy(Vector2.zero);
            var deadEnemy = CreateEnemy(Vector2.zero);
            _world.Add<DeadTag>(deadEnemy);
            _targets.Add(enemy, Vector2.zero);
            _targets.Add(deadEnemy, Vector2.zero);
            _world.Create(new RainOfArrowsImpactRequest
            {
                Position = Vector2.zero,
                Radius = 1f,
                DamageHits = 1
            });
            _world.Create(new RainOfArrowsImpactRequest
            {
                Position = Vector2.zero,
                Radius = 1f,
                DamageHits = 2
            });

            new ApplyRainOfArrowsImpactsSystem(_world, _targets).Update(default);

            Assert.That(_world.Get<HitRequest>(enemy).Count, Is.EqualTo(3));
            Assert.That(_world.Has<HitRequest>(deadEnemy), Is.False);

            _targets.Clear();
            _targets.Add(enemy, new Vector2(5f, 0f));
            _world.Create(new RainOfArrowsImpactRequest
            {
                Position = Vector2.zero,
                Radius = 1f,
                DamageHits = 1
            });
            new ApplyRainOfArrowsImpactsSystem(_world, _targets).Update(default);
            Assert.That(_world.Get<HitRequest>(enemy).Count, Is.EqualTo(3));
        }

        [Test]
        public void ProjectilePositionUsesImpactTimeAndArrivalCreatesImpactRequest()
        {
            var view = new GameObject("Rain Arrow Test View");
            try
            {
                var projectile = _world.Create(
                    new RainOfArrowsProjectile
                    {
                        ImpactPoint = Vector2.zero,
                        ImpactRadius = 1f,
                        Speed = 4f,
                        DamageHits = 1,
                        ImpactTime = 1d
                    },
                    new Position { Value = new Vector2(0f, 4f) },
                    new GameObjectReference(view));
                var system = new AdvanceRainOfArrowsProjectilesSystem(_world);

                system.Update(new SystemState { Time = 0.5d, DeltaTime = 0.5f });
                Assert.That(_world.Get<Position>(projectile).Value.y, Is.EqualTo(2f));
                Assert.That(_world.Has<DeadTag>(projectile), Is.False);

                system.Update(new SystemState { Time = 1d, DeltaTime = 0.5f });
                Assert.That(_world.Get<Position>(projectile).Value, Is.EqualTo(Vector2.zero));
                Assert.That(_world.Has<DeadTag>(projectile), Is.True);
                Assert.That(Count<RainOfArrowsImpactRequest>(), Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(view);
            }
        }

        [Test]
        public void ExpiredZoneReleasesConcurrencySlotAndEntersDestroyPipeline()
        {
            var owner = _world.Create(CreateAbility(activeZones: 1));
            var zone = _world.Create(new RainOfArrowsZone
            {
                Owner = owner,
                EndTime = 4d,
                ProjectileCount = 12,
                LaunchedProjectiles = 12
            });

            new ExpireRainOfArrowsZonesSystem(_world)
                .Update(new SystemState { Time = 4d });

            Assert.That(_world.Get<RainOfArrowsAbility>(owner).ActiveZones, Is.Zero);
            Assert.That(_world.Has<DeadTag>(zone), Is.True);
        }

        private RainOfArrowsAbility CreateAbility(
            double nextActivationTime = 0d,
            int activeZones = 0) =>
            new()
            {
                NextActivationTime = nextActivationTime,
                CooldownDuration = _config.CooldownDuration,
                Duration = _config.Duration,
                MaxConcurrentZones = _config.MaxConcurrentZones,
                ActiveZones = activeZones,
                TargetingRange = _config.GetTargetingRange(_grid),
                ZoneRadius = _config.GetZoneRadius(_grid),
                ImpactRadius = _config.GetImpactRadius(_grid),
                ProjectileCount = _config.ProjectileCount,
                DamageHits = _config.DamageHits,
                ProjectileSpeed = _config.ProjectileSpeed
            };

        private Entity CreateEnemy(Vector2 position) =>
            _world.Create(
                new Position { Value = position },
                new EnemyDefense(),
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Enemy
                });

        private int Count<T>()
        {
            var description = new QueryDescription().WithAll<T>();
            var count = 0;
            _world.Query(in description, (ref T _) => count++);
            return count;
        }
    }
}
