using Arch.Core;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Health;
using EntityLifetime = _Project.Gameplay.Features.Lifetime.Lifetime;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Common;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;
using quaternion = Unity.Mathematics.quaternion;
using _Project.Gameplay.Features.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using _Project.Gameplay.Features.Rotation;
using Unity.Mathematics;

namespace _Project.Gameplay.Features.Shooting.Systems
{
    public sealed class ShootProjectilesSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PlayerTag, Position>();

        private readonly ProjectileConfig _settings;
        private readonly Camera _worldCamera;
        private double _nextShotTime;

        public ShootProjectilesSystem(
            World world,
            ProjectileConfig settings,
            Camera worldCamera
        ) : base(world)
        {
            _settings = settings;
            _worldCamera = worldCamera;
        }

        public override void Update(in SystemState state)
        {
            if (state.Time < _nextShotTime || !IsShootPressed())
            {
                return;
            }

            var hasPlayer = false;
            var playerPosition = Vector2.zero;
            World.Query(in _description, (ref Position position) =>
            {
                if (hasPlayer)
                {
                    return;
                }

                hasPlayer = true;
                playerPosition = position.Value;
            });

            if (!hasPlayer || !TryGetMouseDirection(playerPosition, out var direction))
            {
                return;
            }

            SpawnProjectile(playerPosition, direction);
            _nextShotTime = state.Time + _settings.FireCooldown;
        }

        private void SpawnProjectile(Vector2 playerPosition, Vector2 direction)
        {
            var position = playerPosition + direction * (_settings.Radius + 0.4f);
            var projectileEntity = World.Create(
                new ProjectileTag(),
                new Position { Value = position },
                new RotationComponent { Value = quaternion.identity },
                new Direction { Value = direction },
                new MoveSpeed { Value = _settings.Speed },
                new Velocity(),
                new AngularVelocity() { Value = new float3(0, 0, 40) },
                new CircleCollider
                {
                    Radius = _settings.Radius,
                    Kind = ColliderKind.Trigger,
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Projectile
                },
                new CollisionEvents(),
                new DamageOnCollision
                {
                    Amount = _settings.Damage,
                    EventType = CollisionEventType.TriggerEnter
                },
                new EntityLifetime { Value = _settings.Lifetime });

            if (_settings.Prefab == null)
            {
                return;
            }

            var view = Object.Instantiate(_settings.Prefab);
            view.name = "Projectile View";
            World.Add(projectileEntity, new GameObjectReference(view));
        }

        private static bool IsShootPressed()
        {
            return Keyboard.current?.spaceKey.wasPressedThisFrame == true ||
                   Mouse.current?.leftButton.wasPressedThisFrame == true;
        }

        private bool TryGetMouseDirection(Vector2 playerPosition, out Vector2 direction)
        {
            direction = Vector2.zero;

            var mouse = Mouse.current;
            if (mouse == null || _worldCamera == null)
            {
                return false;
            }

            var screenPosition = mouse.position.ReadValue();
            var worldPosition = _worldCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -_worldCamera.transform.position.z));
            var difference = (Vector2)worldPosition - playerPosition;
            if (difference == Vector2.zero)
            {
                return false;
            }

            direction = difference.normalized;
            return true;
        }
    }
}
