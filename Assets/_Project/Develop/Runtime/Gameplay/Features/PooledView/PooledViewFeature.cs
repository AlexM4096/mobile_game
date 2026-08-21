using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.PooledView.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.PooledView
{
    public static class PooledViewFeature
    {
        public static void AddPooledViewFeature(this NewArchAppBuilder systems)
        {
            systems.Add<DestroyPositionlessPooledViewsSystem>(SystemRunner.Update);
            systems.Add<ValidatePooledViewsSystem>(SystemRunner.Update);
            systems.Add<ReleaseInvisiblePooledViewsSystem>(SystemRunner.Update);
            systems.Add<AcquireVisiblePooledViewsSystem>(SystemRunner.Update);
            systems.Add<ReleaseDestroyedPooledViewsSystem>(SystemRunner.Update);
            systems.Add<ActivatePooledViewsSystem>(SystemRunner.PreLateUpdate);
        }
    }
}
