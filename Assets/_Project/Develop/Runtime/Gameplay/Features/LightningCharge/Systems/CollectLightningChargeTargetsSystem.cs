using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.LightningCharge.Systems
{
    public sealed class CollectLightningChargeTargetsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, CollisionBody, EnemyDefense>()
                .WithNone<DeadTag>();

        private readonly LightningTargetRegistry _registry;

        public CollectLightningChargeTargetsSystem(
            World world,
            LightningTargetRegistry registry) : base(world)
        {
            _registry = registry;
        }

        public override void Update(in SystemState state)
        {
            _registry.Clear();
            World.Query(in _description, (
                Entity entity,
                ref Position position,
                ref CollisionBody body,
                ref EnemyDefense defense) =>
            {
                if (body.Layer == CollisionLayer.Enemy)
                {
                    _registry.Add(entity, position.Value);
                }
            });
        }
    }
}
