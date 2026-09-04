using System;
using Arch.Core;
using Arch.Unity.Conversion;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Health;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Sword
{
    public sealed class SwordFactory
    {
        private const float FullCircleRadians = Mathf.PI * 2f;

        private readonly World _world;
        private readonly SwordConfig _settings;

        public SwordFactory(World world, SwordConfig settings)
        {
            _world = world;
            _settings = settings;
        }

        public void CreateFor(Entity owner, Transform viewParent)
        {
            if (!_world.IsAlive(owner) || !_world.Has<Position>(owner))
            {
                throw new ArgumentException("A sword owner must be alive and have a position.", nameof(owner));
            }

            var ownerPosition = _world.Get<Position>(owner).Value;
            var halfExtents = _settings.HitboxSize * 0.5f;

            for (var index = 0; index < _settings.WeaponCount; index++)
            {
                var angle = FullCircleRadians * index / _settings.WeaponCount;
                var radialDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var entity = _world.Create(
                    new SwordTag(),
                    new SwordOrbit
                    {
                        Owner = owner,
                        AngleRadians = angle
                    },
                    new Position
                    {
                        Value = ownerPosition + radialDirection * _settings.RotationRadius
                    },
                    new RotationComponent { Value = quaternion.RotateZ(angle) },
                    new CollisionBody
                    {
                        BodyType = ColliderBodyType.Dynamic,
                        Layer = CollisionLayer.PlayerWeapon
                    },
                    new BoxColliderComponent { HalfExtents = halfExtents },
                    new TriggerTag(),
                    new DamageOnCollision
                    {
                        Amount = _settings.Damage,
                        Phase = CollisionPhase.Enter
                    });

                if (_settings.Prefab == null)
                {
                    continue;
                }

                var view = Object.Instantiate(_settings.Prefab, viewParent);
                view.name = $"Sword View {index + 1}";
                view.transform.SetPositionAndRotation(
                    new Vector3(
                        ownerPosition.x + radialDirection.x * _settings.RotationRadius,
                        ownerPosition.y + radialDirection.y * _settings.RotationRadius,
                        0f),
                    Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg));
                _world.Add(entity, new GameObjectReference(view));
            }
        }
    }
}
