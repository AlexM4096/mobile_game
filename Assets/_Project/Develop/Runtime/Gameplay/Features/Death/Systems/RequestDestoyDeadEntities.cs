using _Project.Gameplay.Features.Death;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.EnemySpawn;
using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.Destroy.Systems
{
    public sealed class RequestDestoyDeadEntities : UnitySystemBase
    {
        private static readonly QueryDescription _description = 
            new QueryDescription()
                .WithAll<DeadTag>()
                .WithNone<DestroySelfRequest>();

        private static readonly QueryDescription _deadEnemyDescription = new QueryDescription()
            .WithAll<DeadTag, EnemyDefense>()
            .WithNone<DestroySelfRequest>();

        private readonly EnemyRunStatistics _statistics;

        public RequestDestoyDeadEntities(World world, EnemyRunStatistics statistics = null) : base(world)
        {
            _statistics = statistics;
        }

        public override void Update(in SystemState state)
        {
            if (_statistics != null)
            {
                var time = state.Time;
                World.Query(in _deadEnemyDescription, (ref EnemyDefense _) => _statistics.RecordKill(time));
            }

            World.Add<DestroySelfRequest>(in _description);
        }
    }
}
