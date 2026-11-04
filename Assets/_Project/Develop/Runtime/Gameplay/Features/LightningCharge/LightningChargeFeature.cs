using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.LightningCharge.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.LightningCharge
{
    public static class LightningChargeFeature
    {
        public static void AddLightningChargeFeature(this NewArchAppBuilder systems)
        {
            systems.Add<UpdateLightningChargeStateSystem>(SystemRunner.Update);
            systems.Add<CollectLightningChargeTargetsSystem>(SystemRunner.Update);
            systems.Add<ApplyLightningChargeHitsSystem>(SystemRunner.Update);
            systems.Add<UpdateLightningChargeSwordViewsSystem>(SystemRunner.Update);
        }
    }
}
