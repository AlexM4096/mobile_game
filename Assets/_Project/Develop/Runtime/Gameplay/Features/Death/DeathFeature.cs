using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Death.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;
using _Project.Gameplay.Features.Destroy.Systems;

namespace _Project.Gameplay.Features.Death
{
    public static class DeathFeature
    {
        public static void AddDeathFeature(this NewArchAppBuilder systems)
        {
            systems.Add<MarkDeadByZeroHealthSystem>(SystemRunner.Update);
            systems.Add<MarkDeadByExpiredLifetimeSystem>(SystemRunner.Update);
            systems.Add<RequestDestoyDeadEntities>(SystemRunner.Update);
        }
    }
}