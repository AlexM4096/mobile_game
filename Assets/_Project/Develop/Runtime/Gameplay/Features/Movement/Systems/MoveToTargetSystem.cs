using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using UnityEngine;

namespace _Project.Gameplay.Features.Movement.Systems
{
    /// <summary>Updates velocity for entities following a target.</summary>
    public sealed class MoveToTargetSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, Velocity, MoveSpeed, Target>();

        private MoveToTargetQuery _query;

        public MoveToTargetSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<MoveToTargetQuery, Position, Velocity, MoveSpeed, Target>(
                in _description,
                ref _query);
        }

        private struct MoveToTargetQuery : IForEach<Position, Velocity, MoveSpeed, Target>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(
                ref Position position,
                ref Velocity velocity,
                ref MoveSpeed moveSpeed,
                ref Target target)
            {
                if (target.Entity.IsAlive() && target.Entity.TryGet(out Position targetPosition))
                {
                    var direction = (targetPosition.Value - position.Value).normalized;
                    velocity.Value = direction * moveSpeed.Value;
                    return;
                }

                velocity.Value = Vector2.zero;
            }
        }
    }
}
