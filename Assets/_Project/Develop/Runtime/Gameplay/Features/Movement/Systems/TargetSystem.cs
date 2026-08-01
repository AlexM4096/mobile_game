using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement.Components;
using Arch.Core.Extensions;
using Vector2 = UnityEngine.Vector2;

namespace _Project.Gameplay.Features.Movement.Systems
{
    public sealed class TargetSystem : UnitySystemBase
    {
        private readonly QueryDescription query = 
            new QueryDescription()
                .WithAll<Position, Velocity, MoveSpeed, Target>();

        public TargetSystem(World world) : base(world)
        {

        }

        public override void Update(in SystemState state)
        {
            World.Query(in query, ( 
                ref Position position, 
                ref Velocity velocity,
                ref MoveSpeed moveSpeed,
                ref Target target
            ) =>
            {
                velocity.Value = target.Entity.IsAlive() && target.Entity.TryGet(out Position targetPosition)
                    ? (targetPosition.Value - position.Value).normalized * moveSpeed.Value
                    : Vector2.zero;   
            });
        }
    }
}
