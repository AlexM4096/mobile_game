using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Health.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.Health
{
    public static class HealthFeature
    {
        public static void AddHealthFeature(this NewArchAppBuilder systems)
        {
            systems.Add<ApplyCollisionDamageSystem>(SystemRunner.Update);
            systems.Add<ApplyDamageSystem>(SystemRunner.Update);
            systems.Add<ApplyHealSystem>(SystemRunner.Update);
            systems.Add<UpdateHealthBarsSystem>(SystemRunner.PreLateUpdate);
            systems.Add<DestroyDeadEntitiesSystem>(SystemRunner.PostLateUpdate);
        }
    }
}
