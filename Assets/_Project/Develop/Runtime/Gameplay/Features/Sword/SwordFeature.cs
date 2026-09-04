using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Sword.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Sword
{
    public static class SwordFeature
    {
        public static void AddSwordFeature(this NewArchAppBuilder systems)
        {
            systems.Add<DestroyOrphanedSwordsSystem>(SystemRunner.Update);
            systems.Add<UpdateSwordOrbitSystem>(SystemRunner.Update);
        }
    }
}
