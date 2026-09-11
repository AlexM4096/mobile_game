using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Configs/Gameplay/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float tangentialSpeed = 6f;
        [SerializeField, Min(0f)] private float launchSpeed = 8f;
        [SerializeField, Min(0f)] private float decelerationSpeed = 2f;
        [SerializeField, Min(0f)] private float minimumSpeed = 3f;
        [SerializeField, Min(0f)] private float bounceSpeedIncrease = 1f;
        [SerializeField, Min(0f)] private float maximumSpeed = 12f;

        public float TangentialSpeed => tangentialSpeed;
        public float LaunchSpeed => launchSpeed;
        public float DecelerationSpeed => decelerationSpeed;
        public float MinimumSpeed => minimumSpeed;
        public float BounceSpeedIncrease => bounceSpeedIncrease;
        public float MaximumSpeed => maximumSpeed;
    }
}
