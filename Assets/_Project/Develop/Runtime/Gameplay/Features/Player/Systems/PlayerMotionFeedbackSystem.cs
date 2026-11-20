using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.Player.Systems
{
    public sealed class PlayerMotionFeedbackSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<PlayerTag, PlayerMotion, GameObjectReference>();

        private bool _wasBurstActive;

        public PlayerMotionFeedbackSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _description, (
                ref PlayerMotion motion,
                ref GameObjectReference gameObjectReference) =>
            {
                var burstActive = motion.Mode == PlayerMotionMode.Flight && motion.LaunchBurstRemaining > 0f;
                if (gameObjectReference.GameObject == null ||
                    !gameObjectReference.GameObject.TryGetComponent<PlayerMotionFeedbackView>(out var feedback))
                {
                    return;
                }

                feedback.SetComboFire(motion.ComboFireActive);
                if (burstActive != _wasBurstActive)
                {
                    if (burstActive)
                    {
                        feedback.PlayLaunchBurst();
                    }
                    else
                    {
                        feedback.EndLaunchBurst();
                    }
                }

                _wasBurstActive = burstActive;
            });
        }
    }
}
