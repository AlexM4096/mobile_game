using Arch.Core;
using Arch.Unity.Toolkit;
using UnityEngine;

namespace _Project.Gameplay.Features.Sword.Systems
{
    public sealed class AdvanceSwordOrbitSystem : UnitySystemBase
    {
        private const float FullCircleRadians = Mathf.PI * 2f;

        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<SwordAbility>();

        private readonly SwordConfig _settings;

        public AdvanceSwordOrbitSystem(World world, SwordConfig settings) : base(world)
        {
            _settings = settings;
        }

        public override void Update(in SystemState state)
        {
            var direction = (float)_settings.Direction;
            var deltaTime = state.DeltaTime;
            World.Query(in _description, (ref SwordAbility ability) =>
            {
                var angularSpeed = ability.BaseRotationSpeedDegreesPerSecond *
                                   ability.RotationSpeedMultiplier *
                                   Mathf.Deg2Rad;
                ability.OrbitPhaseRadians = Mathf.Repeat(
                    ability.OrbitPhaseRadians + angularSpeed * direction * deltaTime,
                    FullCircleRadians);
            });
        }
    }
}
