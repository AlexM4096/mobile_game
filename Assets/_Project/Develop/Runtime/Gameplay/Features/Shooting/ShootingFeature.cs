using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Shooting.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Shooting
{
    public static class ShootingFeature
    {
        public static void AddShootingFeature(this NewArchAppBuilder systems)
        {
            systems.Add<ShootProjectilesSystem>(SystemRunner.PreUpdate);
            systems.Add<ExpireProjectilesSystem>(SystemRunner.Update);
        }
    }
}
