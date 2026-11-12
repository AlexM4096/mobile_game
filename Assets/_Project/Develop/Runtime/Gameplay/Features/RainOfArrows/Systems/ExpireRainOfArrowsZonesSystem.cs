using System.Collections.Generic;
using Arch.Buffer;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Death;

namespace _Project.Gameplay.Features.RainOfArrows.Systems
{
    public sealed class ExpireRainOfArrowsZonesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<RainOfArrowsZone>()
                .WithNone<DeadTag>();

        private readonly List<ExpiredZone> _expiredZones = new();

        public ExpireRainOfArrowsZonesSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            _expiredZones.Clear();
            var currentTime = state.Time;
            World.Query(in _description, (Entity entity, ref RainOfArrowsZone zone) =>
            {
                if (currentTime >= zone.EndTime &&
                    zone.LaunchedProjectiles >= zone.ProjectileCount)
                {
                    _expiredZones.Add(new ExpiredZone(entity, zone.Owner));
                }
            });

            using var commandBuffer = new CommandBuffer();
            for (var index = 0; index < _expiredZones.Count; index++)
            {
                var expired = _expiredZones[index];
                if (World.IsAlive(expired.Owner) &&
                    World.Has<RainOfArrowsAbility>(expired.Owner))
                {
                    ref var ability = ref World.Get<RainOfArrowsAbility>(expired.Owner);
                    ability.ActiveZones = System.Math.Max(0, ability.ActiveZones - 1);
                }

                if (World.IsAlive(expired.Zone) && !World.Has<DeadTag>(expired.Zone))
                {
                    commandBuffer.Add<DeadTag>(expired.Zone);
                }
            }

            commandBuffer.Playback(World, false);
        }

        private readonly struct ExpiredZone
        {
            public ExpiredZone(Entity zone, Entity owner)
            {
                Zone = zone;
                Owner = owner;
            }

            public Entity Zone { get; }
            public Entity Owner { get; }
        }
    }
}
