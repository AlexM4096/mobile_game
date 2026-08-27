using UnityEngine.InputSystem;

namespace _Project.Gameplay.Features.Player
{
    public sealed class PlayerInputSettings
    {
        public PlayerInputSettings(
            InputActionReference pointAction,
            InputActionReference pressAction)
        {
            PointAction = pointAction;
            PressAction = pressAction;
        }

        public InputActionReference PointAction { get; }
        public InputActionReference PressAction { get; }
    }
}
