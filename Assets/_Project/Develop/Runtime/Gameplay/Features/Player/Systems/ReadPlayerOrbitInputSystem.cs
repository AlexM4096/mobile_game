using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Movement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace _Project.Gameplay.Features.Player.Systems
{
    // Input System actions drive orbit control.
    public sealed class ReadPlayerOrbitInputSystem : UnitySystemBase
    {
        private const float MinimumOrbitRadius = 0.1f;
        private const float DirectionEpsilonSquared = 0.00000001f;

        private static readonly QueryDescription _description = 
            new QueryDescription()
                .WithAll<PlayerTag, Position, Direction, Velocity, PlayerMotion>();

        private readonly PlayerInputSettings _inputSettings;
        private readonly PlayerMovementSettings _movementSettings;

        private InputAction _pointAction;
        private InputAction _pressAction;
        private Camera _worldCamera;

        private bool _wasHeld;

        public ReadPlayerOrbitInputSystem(
            World world, 
            PlayerInputSettings inputSettings,
            PlayerMovementSettings movementSettings
        ) : base(world)
        {
            _inputSettings = inputSettings;
            _movementSettings = movementSettings;
        }

        public override void Initialize()
        {
            _worldCamera = Camera.main;

            _pointAction = _inputSettings.PointAction;
            _pressAction = _inputSettings.PressAction;

            _pointAction.Enable();
            _pressAction.Enable();
        }

        private static void StartOrbit(
            Vector2 playerPosition,
            Vector2 orbitCenter,
            Vector2 currentDirection,
            Vector2 currentVelocity,
            ref PlayerMotion motion
        )
        {
            var radius = playerPosition - orbitCenter;
            var radiusMagnitude = radius.magnitude;
            if (radiusMagnitude < MinimumOrbitRadius) return;

            var incomingDirection = currentVelocity.sqrMagnitude > DirectionEpsilonSquared
                ? currentVelocity.normalized
                : currentDirection.sqrMagnitude > DirectionEpsilonSquared
                    ? currentDirection.normalized
                    : Vector2.right;
                    
            var counterClockwiseTangent = new Vector2(-radius.y, radius.x) / radiusMagnitude;

            motion.Mode = PlayerMotionMode.Orbit;
            motion.OrbitCenter = orbitCenter;
            motion.OrbitRadius = radiusMagnitude;
            motion.OrbitDirection = Vector2.Dot(counterClockwiseTangent, incomingDirection) >= 0f ? 1f : -1f;
        }

        private void LaunchFromOrbit(
            Vector2 playerPosition,
            ref Direction direction,
            ref Velocity velocity,
            ref PlayerMotion motion
        )
        {
            var radiusDirection = (playerPosition - motion.OrbitCenter).normalized;
            var tangent = new Vector2(-radiusDirection.y, radiusDirection.x) * motion.OrbitDirection;
            direction.Value = tangent;
            velocity.Value = tangent * _movementSettings.LaunchSpeed;
            motion.Mode = PlayerMotionMode.Flight;
        }

        private bool TryGetWorldPoint(Vector2 screenPosition, out Vector2 worldPoint)
        {
            worldPoint = default;
            var ray = _worldCamera.ScreenPointToRay(screenPosition);
            if (Mathf.Abs(ray.direction.z) <= Mathf.Epsilon) return false;

            var distance = -ray.origin.z / ray.direction.z;
            if (distance < 0f) return false;

            worldPoint = ray.GetPoint(distance);
            return true;
        }

        private bool IsPointerOverUi()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            if (_pressAction.activeControl?.device is Touchscreen touchscreen)
            {
                return eventSystem.IsPointerOverGameObject(touchscreen.primaryTouch.touchId.ReadValue());
            }

            return eventSystem.IsPointerOverGameObject();
        }      

        public override void Dispose()
        {
            _pointAction.Disable();
            _pressAction.Disable();

            base.Dispose();
        }

        public override void Update(in SystemState state)
        {
            var pressed = _pressAction.WasPressedThisFrame();
            var held = _pressAction.IsPressed();
            var released = _pressAction.WasReleasedThisFrame() || (_wasHeld && !held);
            _wasHeld = held;
            if (!pressed && !released) return;

            var worldPoint = Vector2.zero;
            var hasWorldPoint = pressed && !IsPointerOverUi() && TryGetWorldPoint(_pointAction.ReadValue<Vector2>(), out worldPoint);
            World.Query(in _description, 
            (
                ref Position position, 
                ref Direction direction,
                ref Velocity velocity, 
                ref PlayerMotion motion
            ) =>
            {
                if (pressed && motion.Mode != PlayerMotionMode.Orbit && hasWorldPoint)
                {
                    StartOrbit(position.Value, worldPoint, direction.Value, velocity.Value, ref motion);
                }

                if (released && motion.Mode == PlayerMotionMode.Orbit)
                {
                    LaunchFromOrbit(position.Value, ref direction, ref velocity, ref motion);
                }
            });
        }
    }
}
