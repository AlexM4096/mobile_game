using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Movement;
using UnityEngine;

namespace _Project.Gameplay.Features.Player.Systems
{
    public sealed class SimulatePlayerMotionSystem : UnitySystemBase
    {
        private const float DirectionEpsilonSquared = 0.00000001f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PlayerTag, Position, Direction, Velocity, PlayerMotion, ManualMovementTag>();

        private readonly PlayerMovementConfig _settings;

        public SimulatePlayerMotionSystem(World world, PlayerMovementConfig settings) : base(world)
        {
            _settings = settings;
        }

        public override void Update(in SystemState state)
        {
            var deltaTime = state.DeltaTime;
            World.Query(in _description,
            (
                ref Position position, 
                ref Direction direction,
                ref Velocity velocity, 
                ref PlayerMotion motion
            ) =>
            {
                if (motion.Mode == PlayerMotionMode.Orbit)
                {
                    SimulateOrbit(deltaTime, ref position, ref direction, ref velocity, ref motion);
                    return;
                }

                SimulateFlight(deltaTime, ref position, ref direction, ref velocity);
            });
        }

        private void SimulateOrbit(
            float deltaTime, 
            ref Position position,
            ref Direction direction, 
            ref Velocity velocity, 
            ref PlayerMotion motion
        )
        {
            var radiusDirection = position.Value - motion.OrbitCenter;
            if (radiusDirection.sqrMagnitude <= DirectionEpsilonSquared)
            {
                motion.Mode = PlayerMotionMode.Flight;
                return;
            }

            radiusDirection.Normalize();
            var angle = _settings.RotationSpeed * Mathf.Deg2Rad * motion.OrbitDirection * deltaTime;
            var sine = Mathf.Sin(angle);
            var cosine = Mathf.Cos(angle);
            var rotatedRadius = new Vector2(
                radiusDirection.x * cosine - radiusDirection.y * sine,
                radiusDirection.x * sine + radiusDirection.y * cosine);

            position.Value = motion.OrbitCenter + rotatedRadius * motion.OrbitRadius;
            var tangent = new Vector2(-rotatedRadius.y, rotatedRadius.x) * motion.OrbitDirection;
            direction.Value = tangent;
            velocity.Value = tangent * (_settings.RotationSpeed * Mathf.Deg2Rad * motion.OrbitRadius);
        }

        private void SimulateFlight(
            float deltaTime, 
            ref Position position,
            ref Direction direction, 
            ref Velocity velocity
        )
        {
            var flightDirection = velocity.Value.sqrMagnitude > DirectionEpsilonSquared
                ? velocity.Value.normalized
                : direction.Value.sqrMagnitude > DirectionEpsilonSquared
                    ? direction.Value.normalized
                    : Vector2.right;
            var currentSpeed = velocity.Value.magnitude;
            var speed = currentSpeed < _settings.MinimumSpeed
                ? _settings.MinimumSpeed
                : Mathf.MoveTowards(currentSpeed, _settings.MinimumSpeed,
                    _settings.DecelerationSpeed * deltaTime);

            direction.Value = flightDirection;
            velocity.Value = flightDirection * speed;
            position.Value += velocity.Value * deltaTime;
        }
    }
}
