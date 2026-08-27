using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Player.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Player
{
    public static class PlayerFeature
    {
        public static void AddPlayerControlFeature(this NewArchAppBuilder systems)
        {
            systems.Add<ReadPlayerOrbitInputSystem>(SystemRunner.Update);
            systems.Add<SimulatePlayerMotionSystem>(SystemRunner.Update);
        }

        public static void AddPlayerCollisionResponseFeature(this NewArchAppBuilder systems)
        {
            systems.Add<BouncePlayerOnEnvironmentSystem>(SystemRunner.Update);
        }

        public static void AddPlayerVisualizationFeature(this NewArchAppBuilder systems)
        {
            systems.Add<UpdatePlayerOrbitVisualizationSystem>(SystemRunner.PreLateUpdate);
        }
    }
}
