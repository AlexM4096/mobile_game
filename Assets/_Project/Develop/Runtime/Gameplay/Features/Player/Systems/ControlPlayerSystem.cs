using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Gameplay.Features.Player.Systems
{
    public sealed class ControlPlayerSystem : UnitySystemBase
    {
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PlayerTag, PlayerAim, Direction>();

        public ControlPlayerSystem(World world) : base(world)
        {
        }

        public override void Update(in SystemState state)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
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

            var normalizedDirection = direction == Vector2.zero ? Vector2.zero : direction.normalized;
            World.Query(in _description, (ref Direction movementDirection, ref PlayerAim aim) =>
            {
                movementDirection.Value = normalizedDirection;
                if (normalizedDirection != Vector2.zero)
                {
                    aim.Value = normalizedDirection;
                }
            });
        }
    }
}
