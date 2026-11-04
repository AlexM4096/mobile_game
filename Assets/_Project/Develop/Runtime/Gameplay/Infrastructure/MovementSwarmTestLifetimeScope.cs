using Arch.Core;
using Arch.Unity;
using Arch.Unity.Conversion;
using _Project.Gameplay.Features.AI;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Destroy;
using _Project.Gameplay.Features.EnemySpawn;
using _Project.Gameplay.Features.Health;
using _Project.Gameplay.Features.Lifetime;
using _Project.Gameplay.Features.LightningCharge;
using _Project.Gameplay.Features.Movement;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Progression;
using _Project.Gameplay.Features.Rotation;
using _Project.Gameplay.Features.Shooting;
using _Project.Gameplay.Features.Spear;
using _Project.Gameplay.Features.Sword;
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
using _Project.Utilities;

namespace _Project.Gameplay.Infrastructure
{
    public sealed class MovementSwarmTestLifetimeScope : LifetimeScope
    {
        [SerializeField, InlineEditor] private SwarmConfig swarmConfig;
        [SerializeField, InlineEditor] private EnemySpawnConfig enemySpawnConfig;
        [SerializeField, InlineEditor] private MovementConfig movementConfig;
        [SerializeField, InlineEditor] private PlayerMovementConfig playerMovementConfig;
        [SerializeField, InlineEditor] private PlayerInputConfig playerInputConfig;
        [SerializeField, InlineEditor] private PlayerOrbitVisualizationConfig playerOrbitVisualizationConfig;
        [SerializeField, InlineEditor] private PlayerCameraConfig playerCameraConfig;
        [SerializeField, InlineEditor] private ColliderDebugConfig colliderDebugConfig;
        [SerializeField, InlineEditor] private HealthBarConfig healthBarConfig;
        [SerializeField, InlineEditor] private SwordConfig swordConfig;
        [SerializeField, InlineEditor] private SpearConfig spearConfig;
        [SerializeField, InlineEditor] private LightningChargeConfig lightningChargeConfig;
        [SerializeField, InlineEditor] private ProgressionConfig progressionConfig;
        [SerializeField] private Grid environmentGrid;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private EventSystem eventSystem;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(swarmConfig);
            builder.RegisterInstance(enemySpawnConfig);
            builder.RegisterInstance(movementConfig);
            builder.RegisterInstance(playerMovementConfig);
            builder.RegisterInstance(playerInputConfig);
            builder.RegisterInstance(playerOrbitVisualizationConfig);
            builder.RegisterInstance(playerCameraConfig);
            builder.RegisterInstance(colliderDebugConfig);
            builder.RegisterInstance(healthBarConfig);
            builder.RegisterInstance(swordConfig);
            builder.RegisterInstance(spearConfig);
            builder.RegisterInstance(lightningChargeConfig);
            builder.RegisterInstance(progressionConfig);
            builder.RegisterInstance(environmentGrid);
            builder.RegisterInstance(mainCamera);
            builder.RegisterInstance(eventSystem);
            builder.Register<SwordFactory>(VContainer.Lifetime.Scoped);
            builder.Register<SpearFactory>(VContainer.Lifetime.Scoped);
            builder.Register<LightningTargetRegistry>(VContainer.Lifetime.Scoped);
            builder.Register<LightningArcFactory>(VContainer.Lifetime.Scoped);
            builder.Register<EnemyFactory>(VContainer.Lifetime.Scoped);
            builder.Register<UpgradeOfferGenerator>(VContainer.Lifetime.Scoped);
            builder.Register<UpgradeAvailabilityService>(VContainer.Lifetime.Scoped)
                .As<IUpgradeAvailabilityService>();
            builder.Register<GameplayPauseService>(VContainer.Lifetime.Scoped)
                .As<IGameplayPauseService>();

            var collisionMatrix = CreateCollisionMatrix();
            builder.RegisterInstance(collisionMatrix);

            builder.UseNewArchApp(
                VContainer.Lifetime.Scoped,
                EntityConversion.DefaultWorld,
                systems =>
                {
                    systems.AddPlayerControlFeature();
                    systems.AddEnemySpawnFeature();
                    systems.AddAIFeature();
                    systems.AddMovementFeature();
                    systems.AddSwordFeature();
                    systems.AddCollisionFeature();
                    systems.AddLightningChargeFeature();
                    systems.AddSpearFeature();
                    systems.AddPlayerCollisionResponseFeature();
                    systems.AddRotationFeature();
                    systems.AddHealthFeature();
                    systems.AddDeathFeature();
                    systems.AddProgressionFeature();
                    systems.AddLifetimeFeature();
                    systems.AddDestroyFeature();
                    systems.AddPlayerVisualizationFeature();
                });
            builder.RegisterEntryPoint<SwarmSpawner>();
        }

        internal static CollisionMatrix CreateCollisionMatrix()
        {
            var collisionMatrix = new CollisionMatrix();
            collisionMatrix.SetInteraction(CollisionLayer.Player, CollisionLayer.Enemy, false);
            collisionMatrix.SetInteraction(CollisionLayer.Enemy, CollisionLayer.Enemy, false);
            collisionMatrix.SetInteraction(CollisionLayer.Player, CollisionLayer.Projectile, false);
            collisionMatrix.SetInteraction(CollisionLayer.Projectile, CollisionLayer.Projectile, false);
            collisionMatrix.SetInteraction(CollisionLayer.Default, CollisionLayer.PlayerWeapon, false);
            collisionMatrix.SetInteraction(CollisionLayer.Player, CollisionLayer.PlayerWeapon, false);
            collisionMatrix.SetInteraction(CollisionLayer.Environment, CollisionLayer.PlayerWeapon, false);
            collisionMatrix.SetInteraction(CollisionLayer.Projectile, CollisionLayer.PlayerWeapon, false);
            collisionMatrix.SetInteraction(CollisionLayer.PlayerWeapon, CollisionLayer.PlayerWeapon, false);
            return collisionMatrix;
        }
    }


    internal sealed class SwarmSpawner : IStartable, System.IDisposable
    {
        private readonly World _world;
        private readonly SwarmConfig _configuration;
        private readonly PlayerMovementConfig _playerMovementConfig;
        private readonly PlayerCameraConfig _playerCameraConfig;
        private readonly SwordConfig _swordConfig;
        private readonly SpearConfig _spearConfig;
        private readonly LightningChargeConfig _lightningChargeConfig;
        private readonly SwordFactory _swordFactory;
        private readonly ProgressionConfig _progressionConfig;
        private readonly Grid _environmentGrid;
        private readonly Camera _mainCamera;
        private Transform _spawnedViewsRoot;
        private CinemachineCamera _playerFollowCamera;

        public SwarmSpawner(
            World world,
            SwarmConfig configuration,
            PlayerMovementConfig playerMovementConfig,
            PlayerCameraConfig playerCameraConfig,
            SwordConfig swordConfig,
            SpearConfig spearConfig,
            LightningChargeConfig lightningChargeConfig,
            SwordFactory swordFactory,
            ProgressionConfig progressionConfig,
            Grid environmentGrid,
            Camera mainCamera
        )
        {
            _world = world;
            _configuration = configuration;
            _playerMovementConfig = playerMovementConfig;
            _playerCameraConfig = playerCameraConfig;
            _swordConfig = swordConfig;
            _spearConfig = spearConfig;
            _lightningChargeConfig = lightningChargeConfig;
            _swordFactory = swordFactory;
            _progressionConfig = progressionConfig;
            _environmentGrid = environmentGrid;
            _mainCamera = mainCamera;
        }

        public void Start()
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            _spawnedViewsRoot = new GameObject("Spawned Player Views").transform;
            var targetEntity = _world.Create(
                new PlayerTag(),
                new SwordAbility
                {
                    WeaponCount = _swordConfig.WeaponCount,
                    BaseRotationSpeedDegreesPerSecond =
                        _swordConfig.RotationSpeedDegreesPerSecond,
                    RotationSpeedMultiplier = 1f,
                    OrbitPhaseRadians = 0f
                },
                new CharacterProgress
                {
                    Level = 1,
                    Experience = 0,
                    RequiredExperience = _progressionConfig.BaseExperience,
                    PendingSelections = 0
                },
                new UpgradeRanks(),
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

            var cameraTarget = new GameObject("Player Camera Target");
            cameraTarget.transform.SetParent(_spawnedViewsRoot, false);
            cameraTarget.transform.position = targetView.transform.position;
            _world.Add(targetEntity, new PlayerCameraTargetReference
            {
                Transform = cameraTarget.transform
            });

            _swordFactory.CreateFor(targetEntity, _spawnedViewsRoot);
            CreatePlayerFollowCamera(cameraTarget.transform);
        }

        private bool ValidateConfiguration()
        {
            if (_progressionConfig == null)
            {
                Debug.LogError("Movement swarm test requires progression settings.");
                return false;
            }

            if (!_progressionConfig.TryValidate(out var progressionError))
            {
                Debug.LogError($"Movement swarm test has invalid progression settings. {progressionError}");
                return false;
            }

            if (!TryValidateSpearConfiguration(_spearConfig, out var spearError))
            {
                Debug.LogError($"Movement swarm test has invalid Spear settings. {spearError}");
                return false;
            }

            if (_lightningChargeConfig == null)
            {
                Debug.LogError(
                    "Movement swarm test has invalid Lightning Charge settings. " +
                    "Lightning Charge settings are missing.");
                return false;
            }

            if (!_lightningChargeConfig.TryValidate(_environmentGrid, out var lightningError))
            {
                Debug.LogError(
                    $"Movement swarm test has invalid Lightning Charge settings. {lightningError}");
                return false;
            }

            if (_configuration.PlayerPrefab == null)
            {
                Debug.LogError("Movement swarm test requires a player prefab.");
                return false;
            }

            if (_swordConfig == null || _swordConfig.Prefab == null)
            {
                Debug.LogError("Movement swarm test requires a sword prefab.");
                return false;
            }

            if (_swordConfig.RotationSpeedDegreesPerSecond <= 0f ||
                _swordConfig.RotationRadius <= 0f ||
                _swordConfig.WeaponCount < 1 ||
                _swordConfig.Damage <= 0f ||
                _swordConfig.HitboxSize.x <= 0f ||
                _swordConfig.HitboxSize.y <= 0f ||
                (_swordConfig.Direction != SwordRotationDirection.Clockwise &&
                 _swordConfig.Direction != SwordRotationDirection.CounterClockwise))
            {
                Debug.LogError("Movement swarm test has invalid sword settings.");
                return false;
            }

            return true;
        }

        internal static bool TryValidateSpearConfiguration(
            SpearConfig configuration,
            out string error)
        {
            if (configuration == null)
            {
                error = "Spear settings are missing.";
                return false;
            }

            return configuration.TryValidate(out error);
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

        private void CreatePlayerFollowCamera(Transform followTarget)
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
            _playerFollowCamera.Follow = followTarget;

            var positionComposer = cameraObject.AddComponent<CinemachinePositionComposer>();
            positionComposer.CameraDistance = Mathf.Abs(
                _mainCamera.transform.position.z - followTarget.position.z);
            positionComposer.Damping = new Vector3(
                _playerCameraConfig.Damping,
                _playerCameraConfig.Damping,
                0f);
            // positionComposer.Lookahead = new LookaheadSettings()
            // {
            //     Enabled = true,
            //     Time = 0.5f,
            //     Smoothing = 5
            // };

        }
    }
}
