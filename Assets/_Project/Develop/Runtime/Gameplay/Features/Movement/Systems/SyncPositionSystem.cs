using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.Movement.Systems
{
    /// <summary>Copies ECS positions to hybrid GameObject views.</summary>
    public sealed class SyncPositionSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, GameObjectReference>();

        private SyncPositionQuery _query;

        public SyncPositionSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<SyncPositionQuery, Position, GameObjectReference>(
                in _description,
                ref _query);
        }

        private struct SyncPositionQuery : IForEach<Position, GameObjectReference>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(ref Position position, ref GameObjectReference reference)
            {
                reference.GameObject.transform.position = position.Value;
            }
        }
    }
}
