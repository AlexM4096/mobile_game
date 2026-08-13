using Arch.Core;
using Arch.Unity;
using Arch.Unity.Conversion;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Health.Systems;
using _Project.Gameplay.Features.Lifetime;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Shooting;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Gameplay.Infrastructure
{
    public sealed class MovementSwarmTestLifetimeScope : LifetimeScope
    {
        [Header("Swarm")]
        [SerializeField, Min(1)] private int enemyCount = 500;
        [SerializeField, Min(0f)] private float enemySpeed = 3f;
        [SerializeField, Min(0f)] private float arrivalDistance = 0.05f;
        [SerializeField, Min(0f)] private float innerSpawnRadius = 7f;
        [SerializeField, Min(0f)] private float outerSpawnRadius = 11f;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField, Min(1f)] private float enemyHealth = 100f;
        [SerializeField, Min(1f)] private float targetHealth = 100f;

        [Header("Collision")]
        [SerializeField, Min(0.01f)] private float enemyColliderRadius = 0.25f;
        [SerializeField, Min(0.01f)] private float targetColliderRadius = 0.35f;

        [Header("Health Bars")]
        [SerializeField] private Vector3 healthBarWorldOffset = new(0f, 0.45f, 0f);
        [SerializeField, Min(1f)] private float healthBarWidth = 56f;
        [SerializeField, Min(1f)] private float healthBarHeight = 7f;

        [Header("Player Movement")]
        [SerializeField, Min(0f)] private float targetSpeed = 2f;

        [Header("Shooting")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0f)] private float projectileSpeed = 9f;
        [SerializeField, Min(0f)] private float projectileDamage = 25f;
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.12f;
        [SerializeField, Tooltip("-1 means infinite lifetime.")]
        private float projectileLifetime = 1.5f;
        [SerializeField, Min(0f)] private float fireCooldown = 0.2f;

        protected override void Configure(IContainerBuilder builder)
        {
            var configuration = new SwarmConfiguration(
                enemyCount,
                enemySpeed,
                enemyHealth,
                targetHealth,
                enemyColliderRadius,
                targetColliderRadius,
                innerSpawnRadius,
                outerSpawnRadius,
                randomSeed,
                enemyPrefab,
                playerPrefab);

            builder.RegisterInstance(configuration);
            builder.RegisterInstance(new MovementSettings(arrivalDistance));
            builder.RegisterInstance(new PlayerMovementSettings(targetSpeed));
            builder.RegisterInstance(new ProjectileSettings(
                projectilePrefab,
                projectileSpeed,
                projectileDamage,
                projectileRadius,
                projectileLifetime,
                fireCooldown));
            builder.RegisterInstance(new HealthBarSettings(
                healthBarWorldOffset,
                healthBarWidth,
                healthBarHeight));

            var collisionMatrix = new CollisionMatrix();
            collisionMatrix.SetInteraction(CollisionLayer.Player, CollisionLayer.Enemy, false);
            collisionMatrix.SetInteraction(CollisionLayer.Player, CollisionLayer.Projectile, false);
            collisionMatrix.SetInteraction(CollisionLayer.Projectile, CollisionLayer.Projectile, false);
            builder.RegisterInstance(collisionMatrix);

            builder.UseNewArchApp(
                VContainer.Lifetime.Scoped,
                EntityConversion.DefaultWorld,
                systems =>
                {
                    systems.AddPlayerFeature();
                    systems.AddMovementFeature();
                    systems.AddCollisionFeature();
                    systems.AddHealthFeature();
                    systems.AddShootingFeature();
                    systems.AddLifetimeFeature();
                });
            builder.RegisterEntryPoint<SwarmSpawner>();
        }
    }

    internal readonly struct SwarmConfiguration
    {
        public SwarmConfiguration(
            int enemyCount,
            float enemySpeed,
            float enemyHealth,
            float targetHealth,
            float enemyColliderRadius,
            float targetColliderRadius,
            float innerSpawnRadius,
            float outerSpawnRadius,
            int randomSeed,
            GameObject enemyPrefab,
            GameObject playerPrefab)
        {
            EnemyCount = enemyCount;
            EnemySpeed = enemySpeed;
            EnemyHealth = enemyHealth;
            TargetHealth = targetHealth;
            EnemyColliderRadius = enemyColliderRadius;
            TargetColliderRadius = targetColliderRadius;
            InnerSpawnRadius = innerSpawnRadius;
            OuterSpawnRadius = outerSpawnRadius;
            RandomSeed = randomSeed;
            EnemyPrefab = enemyPrefab;
            PlayerPrefab = playerPrefab;
        }

        public int EnemyCount { get; }
        public float EnemySpeed { get; }
        public float EnemyHealth { get; }
        public float TargetHealth { get; }
        public float EnemyColliderRadius { get; }
        public float TargetColliderRadius { get; }
        public float InnerSpawnRadius { get; }
        public float OuterSpawnRadius { get; }
        public int RandomSeed { get; }
        public GameObject EnemyPrefab { get; }
        public GameObject PlayerPrefab { get; }
    }

    internal sealed class SwarmSpawner : IStartable, System.IDisposable
    {
        private readonly World _world;
        private readonly SwarmConfiguration _configuration;
        private Transform _spawnedViewsRoot;

        public SwarmSpawner(World world, SwarmConfiguration configuration)
        {
            _world = world;
            _configuration = configuration;
        }

        public void Start()
        {
            if (_configuration.EnemyPrefab == null)
            {
                Debug.LogError("Movement swarm test requires an enemy prefab.");
                return;
            }

            if (_configuration.PlayerPrefab == null)
            {
                Debug.LogError("Movement swarm test requires a player prefab.");
                return;
            }

            _spawnedViewsRoot = new GameObject("Spawned Enemy Views").transform;
            var targetEntity = _world.Create(
                new PlayerTag(),
                new PlayerAim { Value = Vector2.right },
                new Position { Value = Vector2.zero },
                new Velocity { Value = Vector2.zero },
                new CircleCollider
                {
                    Radius = _configuration.TargetColliderRadius,
                    Kind = ColliderKind.Solid,
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Player
                },
                new Health
                {
                    Current = _configuration.TargetHealth,
                    Max = _configuration.TargetHealth
                });

            var targetView = Object.Instantiate(_configuration.PlayerPrefab, _spawnedViewsRoot);
            targetView.name = "Player View";
            targetView.transform.position = Vector3.zero;
            _world.Add(targetEntity, new GameObjectReference(targetView));

            var random = new System.Random(_configuration.RandomSeed);
            var minRadius = Mathf.Min(_configuration.InnerSpawnRadius, _configuration.OuterSpawnRadius);
            var maxRadius = Mathf.Max(_configuration.InnerSpawnRadius, _configuration.OuterSpawnRadius);

            for (var index = 0; index < _configuration.EnemyCount; index++)
            {
                var angle = (float)(random.NextDouble() * Mathf.PI * 2f);
                var radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
                var position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var entity = _world.Create(
                    new Position { Value = position },
                    new Velocity { Value = Vector2.zero },
                    new MoveSpeed { Value = _configuration.EnemySpeed },
                    new Target { Entity = targetEntity },
                    new CircleCollider
                    {
                        Radius = _configuration.EnemyColliderRadius,
                        Kind = ColliderKind.Solid,
                        BodyType = ColliderBodyType.Dynamic,
                        Layer = CollisionLayer.Enemy
                    },
                    new Health
                    {
                        Current = Random.Range(1f, _configuration.EnemyHealth),
                        Max = _configuration.EnemyHealth
                    });

                var view = Object.Instantiate(_configuration.EnemyPrefab, _spawnedViewsRoot);
                view.name = $"Enemy View {index + 1}";
                view.transform.position = new Vector3(position.x, position.y, 0f);
                _world.Add(entity, new GameObjectReference(view));
            }
        }

        public void Dispose()
        {
            if (_spawnedViewsRoot != null)
            {
                Object.Destroy(_spawnedViewsRoot.gameObject);
            }
        }
    }
}
