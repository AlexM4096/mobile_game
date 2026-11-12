using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using UnityEngine;
using Random = System.Random;

namespace _Project.Gameplay.Features.RainOfArrows.Systems
{
    public sealed class LaunchRainOfArrowsProjectilesSystem : UnitySystemBase
    {
        private const double TimeEpsilon = 0.000001d;

        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<RainOfArrowsZone, Position>();

        private readonly RainOfArrowsFactory _factory;
        private readonly Random _random = new();
        private readonly List<LaunchRequest> _launches = new();

        public LaunchRainOfArrowsProjectilesSystem(
            World world,
            RainOfArrowsFactory factory) : base(world)
        {
            _factory = factory;
        }

        public override void Update(in SystemState state)
        {
            _launches.Clear();
            var currentTime = state.Time;
            World.Query(in _description, (
                ref RainOfArrowsZone zone,
                ref Position position) =>
            {
                while (zone.LaunchedProjectiles < zone.ProjectileCount &&
                       currentTime + TimeEpsilon >= zone.NextLaunchTime)
                {
                    var launchTime = zone.NextLaunchTime;
                    _launches.Add(new LaunchRequest(
                        SampleImpactPoint(_random, position.Value, zone.ZoneRadius),
                        zone.FallHeight,
                        zone.ProjectileSpeed,
                        zone.ImpactRadius,
                        zone.DamageHits,
                        launchTime + zone.FallHeight / zone.ProjectileSpeed));
                    zone.LaunchedProjectiles++;
                    zone.NextLaunchTime += zone.LaunchInterval;
                }
            });

            for (var index = 0; index < _launches.Count; index++)
            {
                var launch = _launches[index];
                _factory.CreateProjectile(
                    launch.ImpactPoint,
                    launch.FallHeight,
                    launch.Speed,
                    launch.ImpactRadius,
                    launch.DamageHits,
                    launch.ImpactTime,
                    currentTime);
            }
        }

        internal static Vector2 SampleImpactPoint(
            Random random,
            Vector2 center,
            float radius)
        {
            var angle = random.NextDouble() * Math.PI * 2d;
            var distance = Math.Sqrt(random.NextDouble()) * radius;
            return center + new Vector2(
                (float)(Math.Cos(angle) * distance),
                (float)(Math.Sin(angle) * distance));
        }

        private readonly struct LaunchRequest
        {
            public LaunchRequest(
                Vector2 impactPoint,
                float fallHeight,
                float speed,
                float impactRadius,
                int damageHits,
                double impactTime)
            {
                ImpactPoint = impactPoint;
                FallHeight = fallHeight;
                Speed = speed;
                ImpactRadius = impactRadius;
                DamageHits = damageHits;
                ImpactTime = impactTime;
            }

            public Vector2 ImpactPoint { get; }
            public float FallHeight { get; }
            public float Speed { get; }
            public float ImpactRadius { get; }
            public int DamageHits { get; }
            public double ImpactTime { get; }
        }
    }
}
