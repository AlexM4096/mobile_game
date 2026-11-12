using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Player;
using UnityEngine;

namespace _Project.Gameplay.Features.RainOfArrows.Systems
{
    public sealed class ActivateRainOfArrowsSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PlayerTag, Position, RainOfArrowsAbility>()
                .WithNone<DeadTag>();

        private readonly RainOfArrowsConfig _settings;
        private readonly Grid _environmentGrid;
        private readonly RainOfArrowsTargetRegistry _targets;
        private readonly RainOfArrowsFactory _factory;
        private readonly List<PlayerSnapshot> _players = new();
        private string _lastValidationError;

        public ActivateRainOfArrowsSystem(
            World world,
            RainOfArrowsConfig settings,
            Grid environmentGrid,
            RainOfArrowsTargetRegistry targets,
            RainOfArrowsFactory factory) : base(world)
        {
            _settings = settings;
            _environmentGrid = environmentGrid;
            _targets = targets;
            _factory = factory;
        }

        public override void Update(in SystemState state)
        {
            if (!_settings.TryValidate(_environmentGrid, out var error))
            {
                if (_lastValidationError != error)
                {
                    _lastValidationError = error;
                    Debug.LogError(error);
                }

                return;
            }

            _lastValidationError = null;
            _players.Clear();
            World.Query(in _description, (Entity entity, ref Position position) =>
            {
                _players.Add(new PlayerSnapshot(entity, position.Value));
            });

            for (var index = 0; index < _players.Count; index++)
            {
                TryActivate(_players[index], state.Time);
            }
        }

        private void TryActivate(in PlayerSnapshot player, double currentTime)
        {
            if (!World.IsAlive(player.Entity) ||
                !World.Has<RainOfArrowsAbility>(player.Entity))
            {
                return;
            }

            ref var ability = ref World.Get<RainOfArrowsAbility>(player.Entity);
            if (currentTime < ability.NextActivationTime ||
                ability.ActiveZones >= ability.MaxConcurrentZones ||
                !_targets.TryFindBestZoneCenter(
                    player.Position,
                    ability.TargetingRange,
                    ability.ZoneRadius,
                    out var center))
            {
                return;
            }

            _factory.CreateZone(player.Entity, center, in ability, currentTime);
            ability.ActiveZones++;
            ability.NextActivationTime = currentTime + ability.CooldownDuration;
        }

        private readonly struct PlayerSnapshot
        {
            public PlayerSnapshot(Entity entity, Vector2 position)
            {
                Entity = entity;
                Position = position;
            }

            public Entity Entity { get; }
            public Vector2 Position { get; }
        }
    }
}
