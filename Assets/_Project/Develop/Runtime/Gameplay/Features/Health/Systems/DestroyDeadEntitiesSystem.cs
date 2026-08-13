using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using UnityEngine;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class DestroyDeadEntitiesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<DeadTag, GameObjectReference>();

        public DestroyDeadEntitiesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            World.Query(in _description, (ref GameObjectReference reference) =>
            {
                Object.Destroy(reference.GameObject);
            });
        }
    }
}
