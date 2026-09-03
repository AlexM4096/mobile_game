using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Movement;
using UnityEngine;

namespace _Project.Gameplay.Features.Player.Systems
{
    public sealed class BouncePlayerOnEnvironmentSystem : UnitySystemBase
    {
        private const float DirectionEpsilonSquared = 0.00000001f;
        private static readonly QueryDescription _eventDescription =
            new QueryDescription()
                .WithAll<CollisionEvent>();
        private static readonly QueryDescription _playerDescription =
            new QueryDescription()
                .WithAll<PlayerTag, Position, Direction, Velocity, PlayerMotion>();

        private readonly PlayerMovementConfig _settings;
        private readonly Dictionary<Entity, ContactNormals> _normalsByPlayer = new();

        public BouncePlayerOnEnvironmentSystem(
            World world, 
            PlayerMovementConfig settings
        ) : base(world)
        {
            _settings = settings;
        }

        public override void Update(in SystemState state)
        {
            _normalsByPlayer.Clear();

            World.Query(in _eventDescription, (ref CollisionEvent collisionEvent) =>
            {
                if (collisionEvent.Phase != CollisionPhase.Enter ||
                    !World.IsAlive(collisionEvent.First) ||
                    !World.IsAlive(collisionEvent.Second) ||
                    World.Has<TriggerTag>(collisionEvent.First) ||
                    World.Has<TriggerTag>(collisionEvent.Second))
                {
                    return;
                }

                if (IsPlayer(collisionEvent.First) && IsEnvironment(collisionEvent.Second))
                {
                    AddNormal(collisionEvent.First, collisionEvent.Normal);
                }

                if (IsPlayer(collisionEvent.Second) && IsEnvironment(collisionEvent.First))
                {
                    AddNormal(collisionEvent.Second, -collisionEvent.Normal);
                }
            });

            World.Query(in _playerDescription, (
                Entity entity,
                ref Position position,
                ref Direction direction,
                ref Velocity velocity,
                ref PlayerMotion motion) =>
            {
                if (!TryGetEnvironmentNormal(entity, out var normal))
                {
                    return;
                }

                var wasOrbiting = motion.Mode == PlayerMotionMode.Orbit;
                var incomingDirection = wasOrbiting
                    ? GetOrbitTangent(position.Value, motion)
                    : GetFlightDirection(direction.Value, velocity.Value);
                var reflectedDirection = Vector2.Reflect(incomingDirection, normal).normalized;
                var currentSpeed = wasOrbiting ? _settings.LaunchSpeed : velocity.Value.magnitude;
                var boostedSpeed = Mathf.Min(
                    Mathf.Max(currentSpeed, _settings.MinimumSpeed) + _settings.BounceSpeedIncrease,
                    _settings.MaximumSpeed);

                direction.Value = reflectedDirection;
                velocity.Value = reflectedDirection * boostedSpeed;
                motion.Mode = PlayerMotionMode.Flight;
            });
        }

        private bool IsPlayer(Entity entity)
        {
            return World.Has<PlayerTag>(entity);
        }

        private bool IsEnvironment(Entity entity)
        {
            return World.Has<CollisionBody>(entity) &&
                   World.Get<CollisionBody>(entity).Layer == CollisionLayer.Environment;
        }

        private void AddNormal(Entity player, Vector2 normal)
        {
            if (_normalsByPlayer.TryGetValue(player, out var contacts))
            {
                contacts.Sum += normal;
                contacts.Fallback = normal;
                _normalsByPlayer[player] = contacts;
                return;
            }

            _normalsByPlayer.Add(player, new ContactNormals
            {
                Sum = normal,
                Fallback = normal
            });
        }

        private bool TryGetEnvironmentNormal(Entity player, out Vector2 normal)
        {
            if (!_normalsByPlayer.TryGetValue(player, out var contacts))
            {
                normal = Vector2.zero;
                return false;
            }

            normal = contacts.Sum.sqrMagnitude > DirectionEpsilonSquared
                ? contacts.Sum.normalized
                : contacts.Fallback.normalized;
            return normal.sqrMagnitude > DirectionEpsilonSquared;
        }

        private static Vector2 GetOrbitTangent(Vector2 playerPosition, PlayerMotion motion)
        {
            var radiusDirection = (playerPosition - motion.OrbitCenter).normalized;
            return new Vector2(-radiusDirection.y, radiusDirection.x) * motion.OrbitDirection;
        }

        private static Vector2 GetFlightDirection(Vector2 direction, Vector2 velocity)
        {
            if (velocity.sqrMagnitude > DirectionEpsilonSquared) return velocity.normalized;
            return direction.sqrMagnitude > DirectionEpsilonSquared ? direction.normalized : Vector2.right;
        }

        private struct ContactNormals
        {
            public Vector2 Sum;
            public Vector2 Fallback;
        }
    }
}
