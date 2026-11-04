using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Sword;

namespace _Project.Gameplay.Features.LightningCharge.Systems
{
    public sealed class UpdateLightningChargeSwordViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<SwordTag, SwordOrbit, GameObjectReference>();

        private readonly LightningChargeConfig _settings;

        public UpdateLightningChargeSwordViewsSystem(
            World world,
            LightningChargeConfig settings) : base(world)
        {
            _settings = settings;
        }

        public override void Update(in SystemState state)
        {
            var currentTime = state.Time;
            World.Query(in _description, (
                ref SwordOrbit orbit,
                ref GameObjectReference reference) =>
            {
                var isCharged = World.IsAlive(orbit.Owner) &&
                                World.Has<LightningChargeAbility>(orbit.Owner) &&
                                IsCharged(World.Get<LightningChargeAbility>(orbit.Owner), currentTime);

                if (reference.GameObject != null &&
                    reference.GameObject.TryGetComponent<LightningChargeView>(out var view))
                {
                    view.SetCharged(isCharged, _settings.ChargedColor);
                }
            });
        }

        private static bool IsCharged(in LightningChargeAbility ability, double currentTime) =>
            ability.RemainingCharges > 0 && currentTime < ability.ChargeExpiresAt;
    }
}
