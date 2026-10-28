using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    [CreateAssetMenu(fileName = "PlayerCameraConfig", menuName = "Configs/Gameplay/Player Camera Config")]
    public sealed class PlayerCameraConfig : ScriptableObject
    {
        [SerializeField, ToggleLeft] private bool lockToOrbitCenter = true;
        [SerializeField, Min(0f), SuffixLabel("seconds")] private float damping = 0.25f;

        public bool LockToOrbitCenter => lockToOrbitCenter;
        public float Damping => damping;

        private void OnValidate()
        {
            damping = Mathf.Max(0f, damping);
        }
    }
}
