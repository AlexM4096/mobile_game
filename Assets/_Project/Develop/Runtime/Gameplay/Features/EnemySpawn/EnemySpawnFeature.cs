using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.EnemySpawn.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.EnemySpawn
{
    public static class EnemySpawnFeature
    {
        public static void AddEnemySpawnFeature(this NewArchAppBuilder systems)
        {
            systems.Add<SpawnEnemiesSystem>(SystemRunner.Update);
        }
    }
}
