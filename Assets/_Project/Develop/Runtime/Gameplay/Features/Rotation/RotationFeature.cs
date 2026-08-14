using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Rotation.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Rotation
{
    public static class RotationFeature
    {
        public static void AddRotationFeature(this NewArchAppBuilder systems)
        {
            systems.Add<FaceDirectionSystem>(SystemRunner.Update);
            systems.Add<FlipToDirectionSystem>(SystemRunner.Update);
            systems.Add<RotateEntitiesSystem>(SystemRunner.Update);
            systems.Add<SyncRotationSystem>(SystemRunner.PreLateUpdate);
        }
    }
}
