using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.RainOfArrows.Systems
{
    public sealed class ApplyRainOfArrowsImpactsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription().WithAll<RainOfArrowsImpactRequest>();

        private readonly RainOfArrowsTargetRegistry _targets;
        private readonly Dictionary<Entity, int> _hitsByEnemy = new();
        private readonly List<Entity> _requests = new();

        public ApplyRainOfArrowsImpactsSystem(
            World world,
            RainOfArrowsTargetRegistry targets) : base(world)
        {
            _targets = targets;
        }

        public override void Update(in SystemState state)
        {
            _hitsByEnemy.Clear();
            _requests.Clear();
            World.Query(in _description, (
                Entity entity,
                ref RainOfArrowsImpactRequest request) =>
            {
                _requests.Add(entity);
                _targets.AccumulateHits(
                    request.Position,
                    request.Radius,
                    request.DamageHits,
                    _hitsByEnemy);
            });

            using var commandBuffer = new CommandBuffer();
            foreach (var entry in _hitsByEnemy)
            {
                var enemy = entry.Key;
                if (!World.IsAlive(enemy) ||
                    !World.Has<EnemyDefense>(enemy) ||
                    World.Has<DeadTag>(enemy) ||
                    !World.Has<CollisionBody>(enemy) ||
                    World.Get<CollisionBody>(enemy).Layer != CollisionLayer.Enemy)
                {
                    continue;
                }

                if (enemy.TryGet(out HitRequest hitRequest))
                {
                    hitRequest.Count += entry.Value;
                    enemy.Set(hitRequest);
                }
                else
                {
                    commandBuffer.Add(enemy, new HitRequest { Count = entry.Value });
                }
            }

            for (var index = 0; index < _requests.Count; index++)
            {
                if (World.IsAlive(_requests[index]))
                {
                    World.Destroy(_requests[index]);
                }
            }

            commandBuffer.Playback(World, false);
        }

        public override void Dispose()
        {
            _hitsByEnemy.Clear();
            _requests.Clear();
        }
    }
}
