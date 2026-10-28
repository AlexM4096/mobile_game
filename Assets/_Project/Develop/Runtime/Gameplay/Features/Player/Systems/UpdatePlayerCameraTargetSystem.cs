using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using UnityEngine;

namespace _Project.Gameplay.Features.Player.Systems
{
    public sealed class UpdatePlayerCameraTargetSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PlayerTag, Position, PlayerMotion, PlayerCameraTargetReference>();

        private readonly PlayerCameraConfig _settings;

        public UpdatePlayerCameraTargetSystem(World world, PlayerCameraConfig settings) : base(world)
        {
            _settings = settings;
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _description, (
                ref Position position,
                ref PlayerMotion motion,
                ref PlayerCameraTargetReference targetReference) =>
            {
                if (targetReference.Transform == null) return;

                var targetPosition = _settings.LockToOrbitCenter &&
                                     motion.Mode == PlayerMotionMode.Orbit
                    ? motion.OrbitCenter
                    : position.Value;

                targetReference.Transform.position = new Vector3(
                    targetPosition.x,
                    targetPosition.y,
                    targetReference.Transform.position.z);
            });
        }
    }
}
