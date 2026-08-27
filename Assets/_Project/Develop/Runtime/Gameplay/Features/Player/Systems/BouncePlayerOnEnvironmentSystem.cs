using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Movement;
using UnityEngine;

namespace _Project.Gameplay.Features.Player.Systems
{
    public sealed class BouncePlayerOnEnvironmentSystem : UnitySystemBase
    {
        private const float DirectionEpsilonSquared = 0.00000001f;
        private static readonly QueryDescription _description = new QueryDescription()
            .WithAll<PlayerTag, Position, Direction, Velocity, PlayerMotion, CollisionEvents>();
        private readonly PlayerMovementConfig _settings;

        public BouncePlayerOnEnvironmentSystem(World world, PlayerMovementConfig settings) : base(world)
        {
            _settings = settings;
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _description, (ref Position position, ref Direction direction,
                ref Velocity velocity, ref PlayerMotion motion, ref CollisionEvents events) =>
            {
                if (!TryGetEnvironmentNormal(events, out var normal)) return;

                var wasOrbiting = motion.Mode == PlayerMotionMode.Orbit;
                var incomingDirection = wasOrbiting
                    ? GetOrbitTangent(position.Value, motion)
                    : GetFlightDirection(direction.Value, velocity.Value);
                var reflectedDirection = Vector2.Reflect(incomingDirection, normal).normalized;
                var currentSpeed = wasOrbiting ? _settings.LaunchSpeed : velocity.Value.magnitude;
                var boostedSpeed = Mathf.Min(
                    Mathf.Max(currentSpeed, _settings.MinimumSpeed) + _settings.BounceSpeedIncrease,
                    _settings.MaximumSpeed);

                direction.Value = reflectedDirection;
                velocity.Value = reflectedDirection * boostedSpeed;
                motion.Mode = PlayerMotionMode.Flight;
            });
        }

        private bool TryGetEnvironmentNormal(CollisionEvents events, out Vector2 normal)
        {
            normal = Vector2.zero;
            if (events.Items == null) return false;

            var fallbackNormal = Vector2.zero;
            var hasCollision = false;
            foreach (var collisionEvent in events.Items)
            {
                if (collisionEvent.Type != CollisionEventType.CollisionEnter ||
                    !World.IsAlive(collisionEvent.Other) ||
                    !World.Has<CircleCollider>(collisionEvent.Other) ||
                    World.Get<CircleCollider>(collisionEvent.Other).Layer != CollisionLayer.Environment)
                {
                    continue;
                }

                hasCollision = true;
                fallbackNormal = collisionEvent.Normal;
                normal += collisionEvent.Normal;
            }

            if (!hasCollision) return false;
            normal = normal.sqrMagnitude > DirectionEpsilonSquared
                ? normal.normalized
                : fallbackNormal.normalized;
            return normal.sqrMagnitude > DirectionEpsilonSquared;
        }

        private static Vector2 GetOrbitTangent(Vector2 playerPosition, PlayerMotion motion)
        {
            var radiusDirection = (playerPosition - motion.OrbitCenter).normalized;
            return new Vector2(-radiusDirection.y, radiusDirection.x) * motion.OrbitDirection;
        }

        private static Vector2 GetFlightDirection(Vector2 direction, Vector2 velocity)
        {
            if (velocity.sqrMagnitude > DirectionEpsilonSquared) return velocity.normalized;
            return direction.sqrMagnitude > DirectionEpsilonSquared ? direction.normalized : Vector2.right;
        }
    }
}
