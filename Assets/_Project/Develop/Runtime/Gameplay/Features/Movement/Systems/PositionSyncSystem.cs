using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement.Components;

namespace _Project.Gameplay.Features.Movement.Systems
{
    public sealed class PositionSyncSystem : UnitySystemBase
    {
        private readonly QueryDescription query = 
            new QueryDescription()
                .WithAll<Position, GameObjectReference>();

        public PositionSyncSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {      
            World.Query(in query, (
                ref Position position, 
                ref GameObjectReference reference
            ) =>
            {
                var transform = reference.GameObject.transform;
                transform.position = position.Value;
            });
        }
    }
}
