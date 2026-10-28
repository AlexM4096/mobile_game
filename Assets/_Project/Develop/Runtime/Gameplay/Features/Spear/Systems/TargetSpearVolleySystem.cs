using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Player;
using UnityEngine;

namespace _Project.Gameplay.Features.Spear.Systems
{
    public sealed class TargetSpearVolleySystem : UnitySystemBase
    {
        private const float DirectionEpsilonSquared = 0.00000001f;
        private const float FullCircleRadians = Mathf.PI * 2f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position>()
                .WithNone<DeadTag>();

        private readonly SpearConfig _settings;
        private readonly Camera _worldCamera;
        private readonly Grid _environmentGrid;
        private readonly System.Random _random = new();
        private readonly List<PlayerSnapshot> _players = new();
        private readonly List<TargetSnapshot> _targets = new();
        private readonly List<TargetSnapshot> _eligibleTargets = new();
        private string _lastValidationError;

        public TargetSpearVolleySystem(
            World world,
            SpearConfig settings,
            Camera worldCamera,
            Grid environmentGrid) : base(world)
        {
            _settings = settings;
            _worldCamera = worldCamera;
            _environmentGrid = environmentGrid;
        }

        public override void Update(in SystemState state)
        {
            if (!_settings.Enabled)
            {
                return;
            }

            if (!_settings.TryValidate(out var error) ||
                _worldCamera == null ||
                !HasValidGrid())
            {
                error ??= _worldCamera == null
                    ? "Spear requires the gameplay camera."
                    : "Spear requires a gameplay Grid with positive cell dimensions.";
                if (_lastValidationError != error)
                {
                    _lastValidationError = error;
                    Debug.LogError(error);
                }

                return;
            }

            _lastValidationError = null;
            _players.Clear();
            _targets.Clear();

            World.Query(in _description, (Entity entity, ref Position position) =>
            {
                if (World.Has<PlayerTag>(entity) && World.Has<SpearAbilityCooldown>(entity))
                {
                    _players.Add(new PlayerSnapshot(entity, position.Value));
                    return;
                }

                if (!World.Has<EnemyDefense>(entity) || !World.Has<CollisionBody>(entity))
                {
                    return;
                }

                ref var body = ref World.Get<CollisionBody>(entity);
                if (body.Layer == CollisionLayer.Enemy)
                {
                    _targets.Add(new TargetSnapshot(entity, position.Value));
                }
            });

            for (var playerIndex = 0; playerIndex < _players.Count; playerIndex++)
            {
                ActivateIfReady(_players[playerIndex], state.Time);
            }
        }

        private bool HasValidGrid()
        {
            return _environmentGrid != null &&
                   Mathf.Abs(_environmentGrid.cellSize.x) > 0f &&
                   Mathf.Abs(_environmentGrid.cellSize.y) > 0f;
        }

        internal static float CalculateEffectiveRadius(
            Camera worldCamera,
            Vector2 playerPosition,
            float configuredRadius)
        {
            if (worldCamera == null || configuredRadius <= 0f)
            {
                return 0f;
            }

            var worldPosition = new Vector3(playerPosition.x, playerPosition.y, 0f);
            var viewportPosition = worldCamera.WorldToViewportPoint(worldPosition);
            if (viewportPosition.z <= 0f)
            {
                return 0f;
            }

            var bottomLeft = worldCamera.ViewportToWorldPoint(
                new Vector3(0f, 0f, viewportPosition.z));
            var topRight = worldCamera.ViewportToWorldPoint(
                new Vector3(1f, 1f, viewportPosition.z));
            var distanceToViewportEdge = Mathf.Min(
                playerPosition.x - bottomLeft.x,
                topRight.x - playerPosition.x,
                playerPosition.y - bottomLeft.y,
                topRight.y - playerPosition.y);

            return Mathf.Clamp(distanceToViewportEdge, 0f, configuredRadius);
        }

        private void ActivateIfReady(PlayerSnapshot player, double currentTime)
        {
            ref var cooldown = ref World.Get<SpearAbilityCooldown>(player.Entity);
            if (currentTime < cooldown.NextActivationTime)
            {
                return;
            }

            cooldown.NextActivationTime = currentTime + _settings.CooldownDuration;
            CollectEligibleTargets(player.Position);
            ShuffleEligibleTargets();

            for (var projectileIndex = 0;
                 projectileIndex < _settings.ProjectileCount;
                 projectileIndex++)
            {
                var direction = GetDirection(player.Position, projectileIndex);
                World.Create(new SpearShotRequest
                {
                    Owner = player.Entity,
                    Direction = direction,
                    FireTime = currentTime + _settings.TargetingDelay
                });
            }
        }

        private void CollectEligibleTargets(Vector2 playerPosition)
        {
            _eligibleTargets.Clear();
            var radius = CalculateEffectiveRadius(
                _worldCamera,
                playerPosition,
                _settings.TriggerRadius);
            var radiusSquared = radius * radius;

            for (var targetIndex = 0; targetIndex < _targets.Count; targetIndex++)
            {
                var target = _targets[targetIndex];
                var difference = target.Position - playerPosition;
                if (difference.sqrMagnitude > DirectionEpsilonSquared &&
                    difference.sqrMagnitude <= radiusSquared)
                {
                    _eligibleTargets.Add(target);
                }
            }
        }

        private void ShuffleEligibleTargets()
        {
            for (var index = _eligibleTargets.Count - 1; index > 0; index--)
            {
                var swapIndex = _random.Next(index + 1);
                var temporary = _eligibleTargets[index];
                _eligibleTargets[index] = _eligibleTargets[swapIndex];
                _eligibleTargets[swapIndex] = temporary;
            }
        }

        private Vector2 GetDirection(Vector2 playerPosition, int projectileIndex)
        {
            if (_eligibleTargets.Count > 0)
            {
                var targetIndex = projectileIndex < _eligibleTargets.Count
                    ? projectileIndex
                    : _random.Next(_eligibleTargets.Count);
                return (_eligibleTargets[targetIndex].Position - playerPosition).normalized;
            }

            var angle = (float)(_random.NextDouble() * FullCircleRadians);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
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

        private readonly struct TargetSnapshot
        {
            public TargetSnapshot(Entity entity, Vector2 position)
            {
                Entity = entity;
                Position = position;
            }

            public Entity Entity { get; }
            public Vector2 Position { get; }
        }
    }
}
