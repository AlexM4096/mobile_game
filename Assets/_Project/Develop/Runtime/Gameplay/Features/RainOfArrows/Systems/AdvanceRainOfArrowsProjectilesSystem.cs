using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using UnityEngine;

namespace _Project.Gameplay.Features.RainOfArrows.Systems
{
    public sealed class AdvanceRainOfArrowsProjectilesSystem : UnitySystemBase
    {
        private const float ArrivalEpsilonSquared = 0.000001f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<RainOfArrowsProjectile, Position, GameObjectReference>()
                .WithNone<DeadTag>();

        private readonly List<Arrival> _arrivals = new();

        public AdvanceRainOfArrowsProjectilesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            _arrivals.Clear();
            var currentTime = state.Time;
            World.Query(in _description, (
                Entity entity,
                ref RainOfArrowsProjectile projectile,
                ref Position position,
                ref GameObjectReference reference) =>
            {
                var remainingDistance = Mathf.Max(
                    0f,
                    (float)((projectile.ImpactTime - currentTime) * projectile.Speed));
                position.Value = projectile.ImpactPoint + Vector2.up * remainingDistance;
                reference.GameObject.transform.position =
                    new Vector3(position.Value.x, position.Value.y, 0f);

                if (currentTime >= projectile.ImpactTime ||
                    (position.Value - projectile.ImpactPoint).sqrMagnitude <=
                    ArrivalEpsilonSquared)
                {
                    _arrivals.Add(new Arrival(
                        entity,
                        projectile.ImpactPoint,
                        projectile.ImpactRadius,
                        projectile.DamageHits));
                }
            });

            using var commandBuffer = new CommandBuffer();
            for (var index = 0; index < _arrivals.Count; index++)
            {
                var arrival = _arrivals[index];
                World.Create(new RainOfArrowsImpactRequest
                {
                    Position = arrival.Position,
                    Radius = arrival.Radius,
                    DamageHits = arrival.DamageHits
                });
                if (World.IsAlive(arrival.Projectile) &&
                    !World.Has<DeadTag>(arrival.Projectile))
                {
                    commandBuffer.Add<DeadTag>(arrival.Projectile);
                }
            }

            commandBuffer.Playback(World, false);
        }

        private readonly struct Arrival
        {
            public Arrival(Entity projectile, Vector2 position, float radius, int damageHits)
            {
                Projectile = projectile;
                Position = position;
                Radius = radius;
                DamageHits = damageHits;
            }

            public Entity Projectile { get; }
            public Vector2 Position { get; }
            public float Radius { get; }
            public int DamageHits { get; }
        }
    }
}
