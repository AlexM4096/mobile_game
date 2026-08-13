using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using UnityEngine;

namespace _Project.Gameplay.Features.Movement.Systems
{
    /// <summary>Faces hybrid GameObject views along horizontal velocity.</summary>
    public sealed class SyncRotationSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Velocity, GameObjectReference>();

        private SyncRotationQuery _query;

        public SyncRotationSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<SyncRotationQuery, Velocity, GameObjectReference>(
                in _description,
                ref _query
            );
        }

        private struct SyncRotationQuery : IForEach<Velocity, GameObjectReference>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(ref Velocity velocity, ref GameObjectReference reference)
            {
                reference.GameObject.transform.rotation =
                    Quaternion.Euler(0f, velocity.Value.x > 0f ? 0f : 180f, 0f);
            }
        }
    }
}
