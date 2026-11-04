using Arch.Core;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.LightningCharge.Systems
{
    public sealed class UpdateLightningChargeStateSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<LightningChargeAbility>();

        public UpdateLightningChargeStateSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            var currentTime = state.Time;
            World.Query(in _description, (ref LightningChargeAbility ability) =>
            {
                if (ability.RemainingCharges > 0 && currentTime >= ability.ChargeExpiresAt)
                {
                    ability.RemainingCharges = 0;
                    ability.ChargeExpiresAt = 0d;
                }

                if (ability.RemainingCharges > 0 || currentTime < ability.NextActivationTime)
                {
                    return;
                }

                ability.RemainingCharges = ability.ChargesPerActivation;
                ability.ChargeExpiresAt = currentTime + ability.ChargeDuration;
                ability.NextActivationTime = currentTime + ability.CooldownDuration;
            });
        }
    }
}
