using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.EnemySpawn;
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
        private readonly EnemyRunStatistics _statistics;

        public SimulatePlayerMotionSystem(
            World world, PlayerMovementConfig settings, EnemyRunStatistics statistics = null) : base(world)
        {
            _settings = settings;
            _statistics = statistics;
        }

        public override void Update(in SystemState state)
        {
            var deltaTime = state.DeltaTime;
            var comboFire = _statistics != null && _statistics.GetCombo(state.Time).Count >= _settings.FireComboThreshold;
            var speedMultiplier = _settings.GetSpeedMultiplier(comboFire);
            var chargeMultiplier = comboFire ? _settings.FireComboChargeMultiplier : 1f;
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
                    SimulateOrbit(deltaTime, speedMultiplier, chargeMultiplier,
                        ref position, ref direction, ref velocity, ref motion);
                }
                else
                {
                    SimulateFlight(deltaTime, speedMultiplier, ref position, ref direction, ref velocity, ref motion);
                }

                motion.ComboFireActive = comboFire;
            });
        }

        private void SimulateOrbit(
            float deltaTime,
            float speedMultiplier,
            float chargeMultiplier,
            ref Position position,
            ref Direction direction, 
            ref Velocity velocity, 
            ref PlayerMotion motion
        )
        {
            var radiusDirection = position.Value - motion.OrbitCenter;
            if (radiusDirection.sqrMagnitude <= DirectionEpsilonSquared)
            {
                motion.OrbitCharge = 0f;
                motion.Mode = PlayerMotionMode.Flight;
                SimulateFlight(deltaTime, speedMultiplier, ref position, ref direction, ref velocity, ref motion);
                return;
            }

            radiusDirection.Normalize();
            var entrySpeed = motion.OrbitEntryBaseSpeed;
            var chargedSpeed = Mathf.Max(entrySpeed, _settings.GetOrbitSpeed(1f));
            var previousSpeed = Mathf.Lerp(entrySpeed, chargedSpeed, motion.OrbitCharge);
            motion.OrbitCharge = Mathf.MoveTowards(
                motion.OrbitCharge, 1f, deltaTime * chargeMultiplier / _settings.OrbitChargeDuration);
            var tangentialSpeed = Mathf.Lerp(entrySpeed, chargedSpeed, motion.OrbitCharge);
            var burstDistance = AdvanceLaunchBurst(deltaTime, ref motion, out _, out var remainingBonus);
            var traveledDistance = (previousSpeed + tangentialSpeed) * 0.5f * deltaTime + burstDistance;
            var angularDisplacement = traveledDistance * speedMultiplier / motion.OrbitRadius * motion.OrbitDirection;
            var sine = Mathf.Sin(angularDisplacement);
            var cosine = Mathf.Cos(angularDisplacement);
            var rotatedRadius = new Vector2(
                radiusDirection.x * cosine - radiusDirection.y * sine,
                radiusDirection.x * sine + radiusDirection.y * cosine);

            position.Value = motion.OrbitCenter + rotatedRadius * motion.OrbitRadius;
            var tangent = new Vector2(-rotatedRadius.y, rotatedRadius.x) * motion.OrbitDirection;
            direction.Value = tangent;
            velocity.Value = tangent * ((tangentialSpeed + remainingBonus) * speedMultiplier);
        }

        private void SimulateFlight(
            float deltaTime, 
            float speedMultiplier,
            ref Position position,
            ref Direction direction, 
            ref Velocity velocity,
            ref PlayerMotion motion
        )
        {
            var flightDirection = velocity.Value.sqrMagnitude > DirectionEpsilonSquared
                ? velocity.Value.normalized
                : direction.Value.sqrMagnitude > DirectionEpsilonSquared
                    ? direction.Value.normalized
                    : Vector2.right;
            var burstDistance = AdvanceLaunchBurst(deltaTime, ref motion, out var previousBonus, out var remainingBonus);
            var previousMultiplier = _settings.GetSpeedMultiplier(motion.ComboFireActive);
            var currentSpeed = velocity.Value.magnitude / previousMultiplier - previousBonus;
            var speed = currentSpeed < _settings.MinimumSpeed
                ? _settings.MinimumSpeed
                : Mathf.MoveTowards(currentSpeed, _settings.MinimumSpeed,
                    _settings.DecelerationSpeed * deltaTime);

            direction.Value = flightDirection;
            velocity.Value = flightDirection * ((speed + remainingBonus) * speedMultiplier);
            position.Value += flightDirection * ((speed * deltaTime + burstDistance) * speedMultiplier);
        }

        private float AdvanceLaunchBurst(
            float deltaTime, ref PlayerMotion motion, out float previousBonus, out float remainingBonus)
        {
            previousBonus = _settings.GetLaunchBurstBonus(motion.LaunchBurstRemaining);
            var burstStep = Mathf.Min(deltaTime, motion.LaunchBurstRemaining);
            motion.LaunchBurstRemaining -= burstStep;
            remainingBonus = _settings.GetLaunchBurstBonus(motion.LaunchBurstRemaining);
            return (previousBonus + remainingBonus) * 0.5f * burstStep;
        }
    }
}
