using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Destroy.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Destroy
{
    public static class DestroyFeature
    {
        public static void AddDestroyFeature(this NewArchAppBuilder systems)
        {
            systems.Add<DestroyViewsSystem>(SystemRunner.Update);
            systems.Add<DestroyEntitiesSystem>(SystemRunner.Update);
        }
    }
}