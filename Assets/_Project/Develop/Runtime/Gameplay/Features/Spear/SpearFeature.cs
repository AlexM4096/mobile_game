using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Spear.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Spear
{
    public static class SpearFeature
    {
        public static void AddSpearFeature(this NewArchAppBuilder systems)
        {
            systems.Add<TargetSpearVolleySystem>(SystemRunner.PreUpdate);
            systems.Add<FireSpearProjectilesSystem>(SystemRunner.PreUpdate);
            systems.Add<ApplySpearHitsSystem>(SystemRunner.Update);
        }
    }
}
