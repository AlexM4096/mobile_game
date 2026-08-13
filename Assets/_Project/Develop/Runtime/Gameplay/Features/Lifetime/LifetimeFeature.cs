using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Lifetime.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;
namespace _Project.Gameplay.Features.Lifetime
{
    public static class LifetimeFeature
    {
        public static void AddLifetimeFeature(this NewArchAppBuilder systems)
        {
            systems.Add<ExpireEntitiesSystem>(SystemRunner.Update);
        }
    }
}