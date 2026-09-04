using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Collision
{
    public static class CollisionFeature
    {
        public static void AddCollisionFeature(this NewArchAppBuilder systems)
        {
            systems.Add<CleanupCollisionEventsSystem>(SystemRunner.Update);
            systems.Add<DetectCollisionsSystem>(SystemRunner.Update);
#if UNITY_EDITOR
            systems.Add<ColliderDebugSystem>(SystemRunner.PreLateUpdate);
#endif
        }
    }
}
