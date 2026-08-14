using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Movement
{
    /// <summary>Registers the movement simulation and view synchronization pipeline.</summary>
    public static class MovementFeature
    {
        public static void AddMovementFeature(this NewArchAppBuilder systems)
        {
            systems.Add<CalculateVelocitySystem>(SystemRunner.Update);
            systems.Add<MoveEntitiesSystem>(SystemRunner.Update);
            systems.Add<SyncPositionSystem>(SystemRunner.PreLateUpdate);
        }
    }
}
