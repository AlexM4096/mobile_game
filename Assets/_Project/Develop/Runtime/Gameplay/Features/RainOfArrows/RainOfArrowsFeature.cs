using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.RainOfArrows.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.RainOfArrows
{
    public static class RainOfArrowsFeature
    {
        public static void AddRainOfArrowsFeature(this NewArchAppBuilder systems)
        {
            systems.Add<CollectRainOfArrowsTargetsSystem>(SystemRunner.Update);
            systems.Add<ActivateRainOfArrowsSystem>(SystemRunner.Update);
            systems.Add<LaunchRainOfArrowsProjectilesSystem>(SystemRunner.Update);
            systems.Add<AdvanceRainOfArrowsProjectilesSystem>(SystemRunner.Update);
            systems.Add<ApplyRainOfArrowsImpactsSystem>(SystemRunner.Update);
            systems.Add<ExpireRainOfArrowsZonesSystem>(SystemRunner.Update);
            systems.Add<UpdateRainOfArrowsRangeViewSystem>(SystemRunner.PreLateUpdate);
        }
    }
}
