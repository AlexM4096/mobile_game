using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Gameplay.Features.Player
{
    [CreateAssetMenu(fileName = "PlayerInputConfig", menuName = "Configs/Gameplay/Player Input Config")]
    public sealed class PlayerInputConfig : ScriptableObject
    {
        [SerializeField] private InputActionReference pointAction;
        [SerializeField] private InputActionReference pressAction;

        public InputActionReference PointAction => pointAction;
        public InputActionReference PressAction => pressAction;
    }
}