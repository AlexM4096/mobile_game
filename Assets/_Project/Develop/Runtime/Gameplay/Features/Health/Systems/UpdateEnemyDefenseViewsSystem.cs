using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Health.Views;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class UpdateEnemyDefenseViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DefenseViewUpdate, GameObjectReference>();

        public UpdateEnemyDefenseViewsSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            using var commandBuffer = new CommandBuffer();

            World.Query(in _description, (
                Entity entity,
                ref DefenseViewUpdate viewUpdate,
                ref GameObjectReference gameObjectReference) =>
            {
                var viewObject = gameObjectReference.GameObject;
                if (viewObject != null &&
                    viewObject.TryGetComponent<EnemyDefenseView>(out var defenseView))
                {
                    defenseView.ApplyState(
                        viewUpdate.HelmetDurability,
                        viewUpdate.BrokenLayers);
                }

                commandBuffer.Remove<DefenseViewUpdate>(entity);
            });

            commandBuffer.Playback(World, false);
        }
    }
}