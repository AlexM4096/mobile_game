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
            Vector2 position,
            Entity target,
            Random random)
        {
            if (!_world.IsAlive(target) || !_world.Has<Position>(target))
            {
                throw new ArgumentException("Enemy target must be alive and have a position.", nameof(target));
            }

            var speed = SpawnPositionGenerator.ApplyNoise(
                random,
                config.BaseSpeed,
                config.StatNoisePercentage);
            var helmetDurability = EnemyDefenseRules.GetHelmetDurability(
                config.HelmetType,
                config.EnchantedArmor);
            var defense = new EnemyDefense
            {
                HelmetType = config.HelmetType,
                HelmetDurability = helmetDurability,
                MaxHelmetDurability = helmetDurability,
                HasWeapon = config.HasWeapon,
                IsEnchanted = config.EnchantedArmor
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
            if (defenseView == null)
            {
                defenseView = view.AddComponent<EnemyDefenseView>();
            }

            defenseView.Initialize(
                defense.HelmetType,
                defense.HelmetDurability,
                defense.MaxHelmetDurability,
                defense.HasWeapon,
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