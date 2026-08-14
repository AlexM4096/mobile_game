using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Movement;
using UnityEngine;

namespace _Project.Gameplay.Features.AI.Systems
{
    /// <summary>Steers entities toward their current target.</summary>
    public sealed class MoveToTargetSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, Direction, Target>();

        private MoveToTargetQuery _query;

        public MoveToTargetSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<MoveToTargetQuery, Position, Direction, Target>(
                in _description,
                ref _query
            );
        }

        private struct MoveToTargetQuery : IForEach<Position, Direction, Target>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(
                ref Position position,
                ref Direction direction,
                ref Target target
            )
            {
                if (target.Entity.IsAlive() && target.Entity.TryGet(out Position targetPosition))
                {
                    direction.Value = (targetPosition.Value - position.Value).normalized;
                    return;
                }

                direction.Value = Vector2.zero;
            }
        }
    }
}
