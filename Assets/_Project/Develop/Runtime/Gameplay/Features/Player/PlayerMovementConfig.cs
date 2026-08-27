using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Configs/Gameplay/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float rotationSpeed = 90f;
        [SerializeField, Min(0f)] private float launchSpeed = 8f;
        [SerializeField, Min(0f)] private float decelerationSpeed = 2f;
        [SerializeField, Min(0f)] private float minimumSpeed = 3f;
        [SerializeField, Min(0f)] private float bounceSpeedIncrease = 1f;
        [SerializeField, Min(0f)] private float maximumSpeed = 12f;

        public float RotationSpeed => rotationSpeed;
        public float LaunchSpeed => launchSpeed;
        public float DecelerationSpeed => decelerationSpeed;
        public float MinimumSpeed => minimumSpeed;
        public float BounceSpeedIncrease => bounceSpeedIncrease;
        public float MaximumSpeed => maximumSpeed;

        private void OnValidate()
        {
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            minimumSpeed = Mathf.Max(0f, minimumSpeed);
            launchSpeed = Mathf.Max(minimumSpeed, launchSpeed);
            decelerationSpeed = Mathf.Max(0f, decelerationSpeed);
            bounceSpeedIncrease = Mathf.Max(0f, bounceSpeedIncrease);
            maximumSpeed = Mathf.Max(launchSpeed, maximumSpeed);
        }
    }
}