using System;
using Arch.Core;
using Arch.Unity.Conversion;
using _Project.Gameplay.Features.AI;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Views;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Rotation;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;

namespace _Project.Gameplay.Features.EnemySpawn
{
    public sealed class EnemyFactory : IDisposable
    {
        private readonly World _world;
        private Transform _viewsRoot;
        private int _viewIndex;

        public EnemyFactory(World world)
        {
            _world = world;
        }

        public Entity Create(
            EnemyConfig config,
            EnemyEquipmentLoadout loadout,
            EnemyEquipmentVisualConfig equipmentVisuals,
            Vector2 position,
            Random random
        )
        {
            var prefabDefenseView = config.Prefab.GetComponent<EnemyDefenseView>();
            if (prefabDefenseView == null)
            {
                throw new InvalidOperationException(
                    $"Enemy prefab '{config.Prefab.name}' has invalid equipment sockets. " +
                    $"A root {nameof(EnemyDefenseView)} component is required.");
            }

            if (!prefabDefenseView.TryValidate(out var viewError))
            {
                throw new InvalidOperationException(
                    $"Enemy prefab '{config.Prefab.name}' has invalid equipment sockets. " +
                    viewError);
            }

            var speed = SpawnPositionGenerator.ApplyNoise(
                random,
                config.BaseSpeed,
                config.StatNoisePercentage);
            var helmetDurability = EnemyDefenseRules.GetHelmetDurability(
                loadout.HelmetType,
                loadout.IsEnchanted);
            var defense = new EnemyDefense
            {
                HelmetType = loadout.HelmetType,
                HelmetDurability = helmetDurability,
                MaxHelmetDurability = helmetDurability,
                WeaponType = loadout.WeaponType,
                IsEnchanted = loadout.IsEnchanted
            };

            var entity = _world.Create(
                new Position { Value = position },
                new RotationComponent { Value = quaternion.identity },
                new FlipRotationTag(),
                new Direction { Value = Vector2.right },
                new Velocity(),
                new MoveSpeed { Value = speed },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Enemy
                },
                new CircleCollider { Radius = config.ColliderRadius },
                defense);

            var view = Object.Instantiate(config.Prefab, GetViewsRoot());
            view.name = $"{config.Prefab.name} View {++_viewIndex}";
            view.transform.position = new Vector3(position.x, position.y, 0f);

            var defenseView = view.GetComponent<EnemyDefenseView>();
            defenseView.Initialize(
                defense.WeaponType,
                equipmentVisuals.GetWeaponSprite(defense.WeaponType),
                defense.HelmetType,
                equipmentVisuals.GetHelmetSprite(defense.HelmetType),
                defense.HelmetDurability,
                defense.MaxHelmetDurability,
                defense.IsEnchanted);

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

        private Transform GetViewsRoot()
        {
            if (_viewsRoot == null)
            {
                _viewsRoot = new GameObject("Spawned Enemy Views").transform;
            }

            return _viewsRoot;
        }
    }
}
