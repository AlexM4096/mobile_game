using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.Player;
using Unity.Mathematics;
using UnityEngine;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Sword.Systems
{
    public sealed class UpdateSwordOrbitSystem : UnitySystemBase
    {
        private const float FullCircleRadians = Mathf.PI * 2f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<SwordTag, SwordOrbit, Position, RotationComponent>()
                .WithNone<DestroySelfRequest>();

        private readonly SwordConfig _settings;

        public UpdateSwordOrbitSystem(World world, SwordConfig settings) : base(world)
        {
            _settings = settings;
        }

        public override void Update(in SystemState state)
        {
            var angularSpeed = _settings.RotationSpeedDegreesPerSecond * Mathf.Deg2Rad;
            var deltaTime = state.DeltaTime;

            World.Query(in _description, (
                ref SwordOrbit orbit,
                ref Position position,
                ref RotationComponent rotation) =>
            {
                if (!World.IsAlive(orbit.Owner) ||
                    !World.Has<Position>(orbit.Owner) ||
                    !World.Has<PlayerMotion>(orbit.Owner))
                {
                    return;
                }

                ref var ownerPosition = ref World.Get<Position>(orbit.Owner);
                ref var ownerMotion = ref World.Get<PlayerMotion>(orbit.Owner);
                var direction = GetRotationDirection(ownerMotion);

                orbit.AngleRadians = Mathf.Repeat(
                    orbit.AngleRadians + angularSpeed * direction * deltaTime,
                    FullCircleRadians);

                var radialDirection = new Vector2(
                    Mathf.Cos(orbit.AngleRadians),
                    Mathf.Sin(orbit.AngleRadians));
                position.Value = ownerPosition.Value + radialDirection * _settings.RotationRadius;
                rotation.Value = quaternion.RotateZ(orbit.AngleRadians);
            });
        }

        private float GetRotationDirection(PlayerMotion ownerMotion)
        {
            if (ownerMotion.Mode == PlayerMotionMode.Orbit && ownerMotion.OrbitDirection != 0f)
            {
                return Mathf.Sign(ownerMotion.OrbitDirection);
            }

            return (float)_settings.Direction;
        }
    }
}
