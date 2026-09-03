using Arch.Core;
using Arch.Unity;
using Arch.Unity.Conversion;
using _Project.Gameplay.Features.AI;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Lifetime;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Rotation;
using _Project.Gameplay.Features.Shooting;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;
using _Project.Gameplay.Features.Health.Systems;
using RotationComponent = _Project.Gameplay.Features.Common.Rotation;
using quaternion = Unity.Mathematics.quaternion;
using Sirenix.OdinInspector;

namespace _Project.Gameplay.Infrastructure
{
    public sealed class MovementSwarmTestLifetimeScope : LifetimeScope
    {
        [SerializeField, InlineEditor] private SwarmConfig swarmConfig;
        [SerializeField, InlineEditor] private MovementConfig movementConfig;
        [SerializeField, InlineEditor] private PlayerMovementConfig playerMovementConfig;
        [SerializeField, InlineEditor] private PlayerInputConfig playerInputConfig;
        [SerializeField, InlineEditor] private PlayerOrbitVisualizationConfig playerOrbitVisualizationConfig;
        [SerializeField, InlineEditor] private HealthBarConfig healthBarConfig;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private EventSystem eventSystem;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(swarmConfig);
            builder.RegisterInstance(movementConfig);
            builder.RegisterInstance(playerMovementConfig);
            builder.RegisterInstance(playerInputConfig);
            builder.RegisterInstance(playerOrbitVisualizationConfig);
            builder.RegisterInstance(healthBarConfig);
            builder.RegisterInstance(mainCamera);
            builder.RegisterInstance(eventSystem);


            var collisionMatrix = new CollisionMatrix();
            collisionMatrix.SetInteraction(CollisionLayer.Player, CollisionLayer.Enemy, false);
            collisionMatrix.SetInteraction(CollisionLayer.Enemy, CollisionLayer.Enemy, false);
            collisionMatrix.SetInteraction(CollisionLayer.Player, CollisionLayer.Projectile, false);
            collisionMatrix.SetInteraction(CollisionLayer.Projectile, CollisionLayer.Projectile, false);
            builder.RegisterInstance(collisionMatrix);

            builder.UseNewArchApp(
                VContainer.Lifetime.Scoped,
                EntityConversion.DefaultWorld,
                systems =>
                {
                    systems.AddPlayerControlFeature();
                    systems.AddAIFeature();
                    systems.AddMovementFeature();
                    systems.AddCollisionFeature();
                    systems.AddPlayerCollisionResponseFeature();
                    systems.AddRotationFeature();
                    systems.AddHealthFeature();
                    systems.AddDeathFeature();
                    systems.AddLifetimeFeature();
                    systems.AddDestroyFeature();
                    systems.AddPlayerVisualizationFeature();
                });
            builder.RegisterEntryPoint<SwarmSpawner>();
        }
    }


    internal sealed class SwarmSpawner : IStartable, System.IDisposable
    {
        private readonly World _world;
        private readonly SwarmConfig _configuration;
        private readonly PlayerMovementConfig _playerMovementConfig;
        private readonly Camera _mainCamera;
        private Transform _spawnedViewsRoot;
        private CinemachineCamera _playerFollowCamera;

        public SwarmSpawner(
            World world,
            SwarmConfig configuration,
            PlayerMovementConfig playerMovementConfig,
            Camera mainCamera
        )
        {
            _world = world;
            _configuration = configuration;
            _playerMovementConfig = playerMovementConfig;
            _mainCamera = mainCamera;
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
                new PlayerMotion { Mode = PlayerMotionMode.Flight },
                new Position { Value = Vector2.zero },
                new RotationComponent { Value = quaternion.identity },
                new FlipRotationTag(),
                new Direction { Value = Vector2.right },
                new Velocity { Value = Vector2.right * _playerMovementConfig.MinimumSpeed },
                new ManualMovementTag(),
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Dynamic,
                    Layer = CollisionLayer.Player
                },
                new CircleCollider { Radius = _configuration.TargetColliderRadius },
                new Health
                {
                    Current = _configuration.TargetHealth,
                    Max = _configuration.TargetHealth
                });

            var targetView = Object.Instantiate(_configuration.PlayerPrefab, _spawnedViewsRoot);
            targetView.name = "Player View";
            targetView.transform.position = Vector3.zero;
            _world.Add(targetEntity, new GameObjectReference(targetView));
            CreatePlayerFollowCamera(targetView.transform);

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
                    new RotationComponent { Value = quaternion.identity },
                    new FlipRotationTag(),
                    new Direction() { Value = Vector2.right },
                    new Velocity(),
                    new MoveSpeed { Value = Random.Range(1, _configuration.EnemySpeed) },
                    new CollisionBody
                    {
                        BodyType = ColliderBodyType.Dynamic,
                        Layer = CollisionLayer.Enemy
                    },
                    new CircleCollider { Radius = _configuration.EnemyColliderRadius },
                    new Health
                    {
                        Current = Random.Range(1f, _configuration.EnemyHealth),
                        Max = _configuration.EnemyHealth
                    }
                );

                var view = Object.Instantiate(_configuration.EnemyPrefab, _spawnedViewsRoot);
                view.name = $"Enemy View {index + 1}";
                view.transform.position = new Vector3(position.x, position.y, 0f);
                _world.Add(entity, new GameObjectReference(view));
            }
        }

        public void Dispose()
        {
            if (_playerFollowCamera != null)
            {
                Object.Destroy(_playerFollowCamera.gameObject);
            }

            if (_spawnedViewsRoot != null)
            {
                Object.Destroy(_spawnedViewsRoot.gameObject);
            }
        }

        private void CreatePlayerFollowCamera(Transform player)
        {
            if (_mainCamera == null)
            {
                Debug.LogError("Player follow camera requires a main camera.");
                return;
            }

            if (!_mainCamera.TryGetComponent<CinemachineBrain>(out _))
            {
                _mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            var cameraObject = new GameObject("Player Follow Camera");
            cameraObject.transform.SetPositionAndRotation(
                _mainCamera.transform.position,
                _mainCamera.transform.rotation);

            _playerFollowCamera = cameraObject.AddComponent<CinemachineCamera>();
            _playerFollowCamera.Lens = LensSettings.FromCamera(_mainCamera);
            _playerFollowCamera.Follow = player;

            var positionComposer = cameraObject.AddComponent<CinemachinePositionComposer>();
            positionComposer.CameraDistance = Mathf.Abs(
                _mainCamera.transform.position.z - player.position.z);
            positionComposer.Damping = new Vector3(0.25f, 0.25f, 0f);
        }
    }
}
