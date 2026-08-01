using Arch.Core;
using Arch.Unity;
using Arch.Unity.Conversion;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Movement.Components;
using _Project.Gameplay.Features.Movement.Systems;
using _Project.Gameplay.Features.Movement.Views;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private EnemyGameObjectView enemyPrefab;
        [Header("Target Movement")]
        [SerializeField, Min(0f)] private float targetSpeed = 2f;
        [SerializeField, Min(0.01f)] private float targetDirectionChangeInterval = 1.5f;

        protected override void Configure(IContainerBuilder builder)
        {
            var configuration = new SwarmConfiguration(
                enemyCount,
                enemySpeed,
                innerSpawnRadius,
                outerSpawnRadius,
                randomSeed,
                enemyPrefab
            );

            builder.RegisterInstance(configuration);
            builder.RegisterInstance(new MovementSettings(arrivalDistance));
            builder.RegisterInstance(new RandomTargetMovementSettings(
                targetSpeed,
                targetDirectionChangeInterval,
                randomSeed));
            builder.Register<TargetEntityReference>(Lifetime.Scoped);
            builder.UseNewArchApp(Lifetime.Scoped, systems =>
            {
                systems.Add<RandomTargetMovementSystem>(SystemRunner.Update);
                systems.Add<TargetControlSystem>(SystemRunner.Update);
                systems.Add<TargetSystem>(SystemRunner.Update);
                systems.Add<MovementSystem>(SystemRunner.Update);
                systems.Add<PositionSyncSystem>(SystemRunner.PreLateUpdate);
            });
            builder.RegisterEntryPoint<SwarmSpawner>();
        }
    }

    internal readonly struct SwarmConfiguration
    {
        public SwarmConfiguration(
            int enemyCount,
            float enemySpeed,
            float innerSpawnRadius,
            float outerSpawnRadius,
            int randomSeed,
            EnemyGameObjectView enemyPrefab)
        {
            EnemyCount = enemyCount;
            EnemySpeed = enemySpeed;
            InnerSpawnRadius = innerSpawnRadius;
            OuterSpawnRadius = outerSpawnRadius;
            RandomSeed = randomSeed;
            EnemyPrefab = enemyPrefab;
        }

        public int EnemyCount { get; }
        public float EnemySpeed { get; }
        public float InnerSpawnRadius { get; }
        public float OuterSpawnRadius { get; }
        public int RandomSeed { get; }
        public EnemyGameObjectView EnemyPrefab { get; }
    }

    internal sealed class SwarmSpawner : IStartable, System.IDisposable
    {
        private readonly World world;
        private readonly SwarmConfiguration configuration;
        private readonly TargetEntityReference targetEntityReference;
        private Transform spawnedViewsRoot;

        public SwarmSpawner(
            World world,
            SwarmConfiguration configuration,
            TargetEntityReference targetEntityReference)
        {
            this.world = world;
            this.configuration = configuration;
            this.targetEntityReference = targetEntityReference;
        }

        public void Start()
        {
            if (configuration.EnemyPrefab == null)
            {
                Debug.LogError("Movement swarm test requires an enemy prefab.");
                return;
            }

            spawnedViewsRoot = new GameObject("Spawned Enemy Views").transform;
            var targetEntity = world.Create(
                new Position { Value = Vector2.zero },
                new Velocity { Value = Vector2.zero }
            );
            targetEntityReference.Entity = targetEntity;
            var targetView = Object.Instantiate(configuration.EnemyPrefab, spawnedViewsRoot);
            targetView.name = "Target View";
            targetView.transform.position = Vector3.zero;
            world.Add(targetEntity, new GameObjectReference(targetView.gameObject));
            var random = new System.Random(configuration.RandomSeed);
            var minRadius = Mathf.Min(configuration.InnerSpawnRadius, configuration.OuterSpawnRadius);
            var maxRadius = Mathf.Max(configuration.InnerSpawnRadius, configuration.OuterSpawnRadius);

            for (var index = 0; index < configuration.EnemyCount; index++)
            {
                var angle = (float)(random.NextDouble() * Mathf.PI * 2f);
                var radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
                var position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var entity = world.Create(
                    new Position { Value = position },
                    new Velocity { Value = Vector2.zero },
                    new MoveSpeed { Value = configuration.EnemySpeed },
                    new Target { Entity = targetEntity });
                var view = Object.Instantiate(configuration.EnemyPrefab, spawnedViewsRoot);
                view.name = $"Enemy View {index + 1}";
                view.transform.position = new Vector3(position.x, position.y, 0f);
                world.Add(entity, new GameObjectReference(view.gameObject));
            }
        }

        public void Dispose()
        {
            if (spawnedViewsRoot != null)
            {
                Object.Destroy(spawnedViewsRoot.gameObject);
            }
        }
    }

    internal sealed class TargetEntityReference
    {
        public Entity Entity;
    }

    internal readonly struct RandomTargetMovementSettings
    {
        public RandomTargetMovementSettings(float speed, float directionChangeInterval, int randomSeed)
        {
            Speed = speed;
            DirectionChangeInterval = directionChangeInterval;
            RandomSeed = randomSeed;
        }

        public float Speed { get; }
        public float DirectionChangeInterval { get; }
        public int RandomSeed { get; }
    }

    internal sealed class RandomTargetMovementSystem : UnitySystemBase
    {
        private readonly TargetEntityReference targetEntityReference;
        private readonly RandomTargetMovementSettings settings;
        private readonly System.Random random;
        private double nextDirectionChangeTime;

        public RandomTargetMovementSystem(
            World world,
            TargetEntityReference targetEntityReference,
            RandomTargetMovementSettings settings) : base(world)
        {
            this.targetEntityReference = targetEntityReference;
            this.settings = settings;
            random = new System.Random(settings.RandomSeed);
        }

        public override void Update(in SystemState state)
        {
            var targetEntity = targetEntityReference.Entity;
            if (!World.IsAlive(targetEntity) || state.Time < nextDirectionChangeTime)
            {
                return;
            }

            var angle = (float)(random.NextDouble() * Mathf.PI * 2f);
            ref var velocity = ref World.Get<Velocity>(targetEntity);
            velocity.Value = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * settings.Speed;
            nextDirectionChangeTime = state.Time + settings.DirectionChangeInterval;
        }
    }

    internal sealed class TargetControlSystem : UnitySystemBase
    {
        private readonly TargetEntityReference targetEntityReference;
        private readonly RandomTargetMovementSettings settings;

        public TargetControlSystem(
            World world,
            TargetEntityReference targetEntityReference,
            RandomTargetMovementSettings settings) : base(world)
        {
            this.targetEntityReference = targetEntityReference;
            this.settings = settings;
        }

        public override void Update(in SystemState state)
        {
            var targetEntity = targetEntityReference.Entity;
            var keyboard = Keyboard.current;
            if (!World.IsAlive(targetEntity) || keyboard == null)
            {
                return;
            }

            var direction = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                direction.y += 1f;
            }
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                direction.y -= 1f;
            }
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                direction.x -= 1f;
            }
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                direction.x += 1f;
            }

            if (direction == Vector2.zero)
            {
                return;
            }

            ref var velocity = ref World.Get<Velocity>(targetEntity);
            velocity.Value = direction.normalized * settings.Speed;
        }
    }
}
