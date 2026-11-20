using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.EnemySpawn;
using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.Death.Systems
{
    public sealed class AnimateDeadEnemyViewsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description = new QueryDescription()
            .WithAll<DeadTag, DestroySelfRequest, EnemyDefense, Position, GameObjectReference>();

        private readonly EnemyDeathAnimation _animation;
        private readonly EnemyRunStatistics _statistics;
        private int _lastMilestone;

        public AnimateDeadEnemyViewsSystem(
            World world, EnemyDeathAnimation animation, EnemyRunStatistics statistics) : base(world)
        {
            _animation = animation;
            _statistics = statistics;
        }

        public override void Update(in SystemState state)
        {
            using var buffer = new CommandBuffer();
            var milestone = _statistics.ComboMilestones != _lastMilestone;
            _lastMilestone = _statistics.ComboMilestones;
            World.Query(in _description, (
                Entity entity, ref Position position, ref GameObjectReference view, ref EnemyDefense defense) =>
            {
                if (_animation.TryPlay(view.GameObject, position.Value, defense.DefeatDirection, milestone))
                {
                    milestone = false;
                    buffer.Remove<GameObjectReference>(entity);
                }
            });
            buffer.Playback(World, false);
        }
    }
}
