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
            World.Query(in _description, (
                ref SwordOrbit orbit,
                ref Position position,
                ref RotationComponent rotation) =>
            {
                if (!World.IsAlive(orbit.Owner) ||
                    !World.Has<Position>(orbit.Owner) ||
                    !World.Has<PlayerMotion>(orbit.Owner) ||
                    !World.Has<SwordAbility>(orbit.Owner))
                {
                    return;
                }

                ref var ownerPosition = ref World.Get<Position>(orbit.Owner);
                ref var ability = ref World.Get<SwordAbility>(orbit.Owner);
                var angle = Mathf.Repeat(
                    ability.OrbitPhaseRadians +
                    FullCircleRadians * orbit.SlotIndex / ability.WeaponCount,
                    FullCircleRadians);

                var radialDirection = new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle));
                position.Value = ownerPosition.Value + radialDirection * _settings.RotationRadius;
                rotation.Value = quaternion.RotateZ(angle);
            });
        }
    }
}
