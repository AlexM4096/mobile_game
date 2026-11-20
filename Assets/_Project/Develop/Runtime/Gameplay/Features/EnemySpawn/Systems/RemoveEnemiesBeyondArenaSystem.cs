using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.EnemySpawn.Systems
{
    public sealed class RemoveEnemiesBeyondArenaSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description = new QueryDescription()
            .WithAll<EnemyDefense, Position, CircleCollider>()
            .WithNone<DestroySelfRequest>();

        private readonly SceneSpawnZoneProvider _sceneZone;
        private readonly EnemyRunStatistics _statistics;
        private readonly List<Entity> _exitedEnemies = new();

        public RemoveEnemiesBeyondArenaSystem(
            World world, SceneSpawnZoneProvider sceneZone, EnemyRunStatistics statistics) : base(world)
        {
            _sceneZone = sceneZone;
            _statistics = statistics;
        }

        public override void Update(in SystemState state)
        {
            if (!_sceneZone.TryGetLayout(out _, out var arena))
            {
                return;
            }

            var exitX = arena.xMax;
            _exitedEnemies.Clear();
            World.Query(in _description, (Entity entity, ref Position position, ref CircleCollider collider) =>
            {
                if (position.Value.x - collider.Radius > exitX)
                {
                    _exitedEnemies.Add(entity);
                }
            });

            foreach (var enemy in _exitedEnemies)
            {
                World.Add<DestroySelfRequest>(enemy);
            }

            _statistics.RecordEscapes(_exitedEnemies.Count);
        }
    }
}
