using System;
using System.Collections.Generic;
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
        private readonly Dictionary<Entity, Transform> _viewParents = new();

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

            _viewParents[owner] = viewParent;

            for (var index = 0; index < _settings.WeaponCount; index++)
            {
                CreateSlot(owner, index);
            }
        }

        public Entity CreateAdditionalFor(Entity owner, int slotIndex)
        {
            return CreateSlot(owner, slotIndex);
        }

        private Entity CreateSlot(Entity owner, int slotIndex)
        {
            if (!_world.IsAlive(owner) ||
                !_world.Has<Position>(owner) ||
                !_world.Has<SwordAbility>(owner))
            {
                throw new ArgumentException(
                    "A sword owner must be alive and have Position and SwordAbility components.",
                    nameof(owner));
            }

            ref var ability = ref _world.Get<SwordAbility>(owner);
            var ownerPosition = _world.Get<Position>(owner).Value;
            var angle = ability.OrbitPhaseRadians +
                        FullCircleRadians * slotIndex / ability.WeaponCount;
            var radialDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var entity = _world.Create(
                new SwordTag(),
                new SwordOrbit { Owner = owner, SlotIndex = slotIndex },
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
                new BoxColliderComponent { HalfExtents = _settings.HitboxSize * 0.5f },
                new TriggerTag(),
                new DamageOnCollision
                {
                    Amount = _settings.Damage,
                    Phase = CollisionPhase.Enter
                });

            if (_settings.Prefab == null)
            {
                return entity;
            }

            _viewParents.TryGetValue(owner, out var viewParent);
            var view = Object.Instantiate(_settings.Prefab, viewParent);
            view.name = $"Sword View {slotIndex + 1}";
            view.transform.SetPositionAndRotation(
                new Vector3(
                    ownerPosition.x + radialDirection.x * _settings.RotationRadius,
                    ownerPosition.y + radialDirection.y * _settings.RotationRadius,
                    0f),
                Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg));
            _world.Add(entity, new GameObjectReference(view));
            return entity;
        }
    }
}
