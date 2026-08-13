using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Player.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Player
{
    public static class PlayerFeature
    {
        public static void AddPlayerFeature(this NewArchAppBuilder systems)
        {
            systems.Add<ControlPlayerSystem>(SystemRunner.Update);
        }
    }
}
