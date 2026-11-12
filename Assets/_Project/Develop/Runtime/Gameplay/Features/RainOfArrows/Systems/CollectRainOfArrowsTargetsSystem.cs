using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.RainOfArrows.Systems
{
    public sealed class CollectRainOfArrowsTargetsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, EnemyDefense, CollisionBody>()
                .WithNone<DeadTag>();

        private readonly RainOfArrowsTargetRegistry _targets;

        public CollectRainOfArrowsTargetsSystem(
            World world,
            RainOfArrowsTargetRegistry targets) : base(world)
        {
            _targets = targets;
        }

        public override void Update(in SystemState state)
        {
            _targets.Clear();
            World.Query(in _description, (
                Entity entity,
                ref Position position,
                ref CollisionBody body) =>
            {
                if (body.Layer == CollisionLayer.Enemy)
                {
                    _targets.Add(entity, position.Value);
                }
            });
        }
    }
}
