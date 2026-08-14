using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement;
using Unity.Mathematics;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Rotation.Systems
{
    /// <summary>Flips tagged non-spinning entities horizontally along their movement direction.</summary>
    public sealed class FlipToDirectionSystem : UnitySystemBase
    {
        private const float HorizontalDirectionEpsilon = 0.0001f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<RotationComponent, Direction, FlipRotationTag>()
                .WithNone<AngularVelocity>();

        private FlipToDirectionQuery _query;

        public FlipToDirectionSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<FlipToDirectionQuery, RotationComponent, Direction, FlipRotationTag>(
                in _description,
                ref _query
            );
        }

        private struct FlipToDirectionQuery : IForEach<RotationComponent, Direction, FlipRotationTag>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(
                ref RotationComponent rotation,
                ref Direction direction,
                ref FlipRotationTag _
            )
            {
                if (direction.Value.x > HorizontalDirectionEpsilon)
                {
                    rotation.Value = quaternion.identity;
                }
                else if (direction.Value.x < -HorizontalDirectionEpsilon)
                {
                    rotation.Value = quaternion.RotateY(math.PI);
                }
            }
        }
    }
}
