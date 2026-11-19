using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.EnemySpawn;
using Arch.Core;
using Arch.Unity.Toolkit;

namespace _Project.Gameplay.Features.Destroy.Systems
{
    public sealed class RequestDestoyDeadEntities : UnitySystemBase
    {
        private static readonly QueryDescription _description = 
            new QueryDescription()
                .WithAll<DeadTag>()
                .WithNone<DestroySelfRequest>()
                .WithNone<SplitOnDeath>();

        public RequestDestoyDeadEntities(World world) : base(world) { }

        public override void Update(in SystemState state)
        {
            World.Add<DestroySelfRequest>(in _description);
        }
    }
}
