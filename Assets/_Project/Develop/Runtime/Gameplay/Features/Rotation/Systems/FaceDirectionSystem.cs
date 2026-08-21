using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement;
using Unity.Mathematics;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Rotation.Systems
{
    /// <summary>Rotates non-spinning entities toward their full movement direction.</summary>
    public sealed class FaceDirectionSystem : UnitySystemBase
    {
        private const float DirectionEpsilonSquared = 0.00000001f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<RotationComponent, Direction, FaceDirectionTag>()
                .WithNone<AngularVelocity>();

        private FaceDirectionQuery _query;

        public FaceDirectionSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<FaceDirectionQuery, RotationComponent, Direction>(
                in _description, 
                ref _query
            );
        }

        private struct FaceDirectionQuery : IForEach<RotationComponent, Direction>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(ref RotationComponent rotation, ref Direction direction)
            {
                if (direction.Value.sqrMagnitude > DirectionEpsilonSquared)
                {
                    rotation.Value = quaternion.RotateZ(math.atan2(direction.Value.y, direction.Value.x));
                }
            }
        }
    }
}
