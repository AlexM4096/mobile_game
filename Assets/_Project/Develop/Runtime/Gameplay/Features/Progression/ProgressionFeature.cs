using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Progression.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Progression
{
    public static class ProgressionFeature
    {
        public static void AddProgressionFeature(this NewArchAppBuilder systems)
        {
            systems.Add<QueueEnemyExperienceSystem>(SystemRunner.Update);
            systems.Add<ApplyExperienceSystem>(SystemRunner.Update);
            systems.Add<ResolveLevelUpsSystem>(SystemRunner.Update);
            systems.Add<ApplySelectedUpgradeSystem>(SystemRunner.Update);
            systems.Add<UpdateProgressionUiSystem>(SystemRunner.Update);
        }
    }
}
