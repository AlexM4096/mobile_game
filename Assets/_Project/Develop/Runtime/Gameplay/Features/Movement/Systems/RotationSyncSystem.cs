using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement.Components;
using UnityEngine;


namespace _Project.Gameplay.Features.Movement.Systems
{
    public sealed class RotationSyncSystem : UnitySystemBase
    {
        private readonly QueryDescription query = 
            new QueryDescription()
                .WithAll<Velocity, GameObjectReference>();

        public RotationSyncSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {      
            World.Query(in query, (
                ref Velocity velocity, 
                ref GameObjectReference reference
            ) =>
            {
                var transform = reference.GameObject.transform;
                transform.rotation = Quaternion.Euler(0, velocity.Value.x > 0 ? 0 : 180, 0);
            });
        }
    }
}
