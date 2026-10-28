using System;
using Arch.Core;
using Arch.Unity.Conversion;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Rotation;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;
using EntityLifetime = _Project.Gameplay.Features.Lifetime.Lifetime;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.Spear
{
    public sealed class SpearFactory : IDisposable
    {
        internal const float LengthInTiles = 1.5f;

        private readonly World _world;
        private readonly SpearConfig _settings;
        private readonly Grid _environmentGrid;
        private Transform _viewsRoot;
        private int _viewIndex;

        public SpearFactory(World world, SpearConfig settings, Grid environmentGrid)
        {
            _world = world;
            _settings = settings;
            _environmentGrid = environmentGrid;
        }

        public Entity Create(Vector2 origin, Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.00000001f)
            {
                throw new ArgumentException("A spear direction must be non-zero.", nameof(direction));
            }

            if (_environmentGrid == null)
            {
                throw new InvalidOperationException("Spear requires the gameplay Grid.");
            }

            direction.Normalize();
            var angle = math.atan2(direction.y, direction.x);
            var fullSize = GetProjectileSize();
            var entity = _world.Create(
                new SpearProjectile
                {
                    DamageHits = _settings.DamageHits,
                    RemainingDurability = _settings.TotalHealth
                },
                new Position { Value = origin },
                new RotationComponent { Value = quaternion.RotateZ(angle) },
                new Direction { Value = direction },
                new MoveSpeed { Value = _settings.Speed },
                new Velocity(),
                new FaceDirectionTag(),
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.PlayerWeapon
                },
                new BoxColliderComponent { HalfExtents = fullSize * 0.5f },
                new TriggerTag(),
                new EntityLifetime { Value = _settings.ProjectileLifetime });

            if (_settings.Prefab == null)
            {
                return entity;
            }

            var view = Object.Instantiate(_settings.Prefab, GetViewsRoot());
            view.name = $"Spear View {++_viewIndex}";
            view.transform.SetPositionAndRotation(
                new Vector3(origin.x, origin.y, 0f),
                Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg));

            if (view.TryGetComponent<SpearView>(out var spearView))
            {
                spearView.SetSize(fullSize);
            }

            _world.Add(entity, new GameObjectReference(view));
            return entity;
        }

        public void Dispose()
        {
            if (_viewsRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_viewsRoot.gameObject);
            }
            else
            {
                Object.DestroyImmediate(_viewsRoot.gameObject);
            }
        }

        internal Vector2 GetProjectileSize()
        {
            var cellSize = _environmentGrid.cellSize;
            return new Vector2(
                Mathf.Abs(cellSize.x) * LengthInTiles,
                Mathf.Abs(cellSize.y) * _settings.ThicknessMultiplier);
        }

        private Transform GetViewsRoot()
        {
            if (_viewsRoot == null)
            {
                _viewsRoot = new GameObject("Spawned Spear Views").transform;
            }

            return _viewsRoot;
        }
    }
}
