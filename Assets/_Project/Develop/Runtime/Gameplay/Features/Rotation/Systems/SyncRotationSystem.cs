using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using UnityEngine;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Rotation.Systems
{
    /// <summary>Copies ECS rotations to hybrid GameObject views.</summary>
    public sealed class SyncRotationSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<RotationComponent, GameObjectReference>();

        private SyncRotationQuery _query;

        public SyncRotationSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.InlineQuery<SyncRotationQuery, RotationComponent, GameObjectReference>(
                in _description,
                ref _query
            );
        }

        private struct SyncRotationQuery : IForEach<RotationComponent, GameObjectReference>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Update(ref RotationComponent rotation, ref GameObjectReference reference)
            {
                var value = rotation.Value.value;
                reference.GameObject.transform.rotation = new Quaternion(
                    value.x, 
                    value.y, 
                    value.z, 
                    value.w
                );
            }
        }
    }
}
