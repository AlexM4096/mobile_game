using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.AI.Systems;
using NewArchAppBuilder = Arch.Unity.ArchVContainerExtensions.NewArchAppBuilder;

namespace _Project.Gameplay.Features.AI
{
    public static class AIFeature
    {
        public static void AddAIFeature(this NewArchAppBuilder systems)
        {
            systems.Add<MoveToTargetSystem>(SystemRunner.Update);
        }
    }
}
