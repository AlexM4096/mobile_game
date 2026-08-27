namespace _Project.Gameplay.Features.Player
{
    public sealed class PlayerMovementSettings
    {
        public PlayerMovementSettings(
            float rotationSpeed,
            float launchSpeed,
            float decelerationSpeed,
            float minimumSpeed,
            float bounceSpeedIncrease,
            float maximumSpeed)
        {
            RotationSpeed = UnityEngine.Mathf.Max(0f, rotationSpeed);
            MinimumSpeed = UnityEngine.Mathf.Max(0f, minimumSpeed);
            LaunchSpeed = UnityEngine.Mathf.Max(MinimumSpeed, launchSpeed);
            DecelerationSpeed = UnityEngine.Mathf.Max(0f, decelerationSpeed);
            BounceSpeedIncrease = UnityEngine.Mathf.Max(0f, bounceSpeedIncrease);
            MaximumSpeed = UnityEngine.Mathf.Max(LaunchSpeed, maximumSpeed);
        }

        public float RotationSpeed { get; }
        public float LaunchSpeed { get; }
        public float DecelerationSpeed { get; }
        public float MinimumSpeed { get; }
        public float BounceSpeedIncrease { get; }
        public float MaximumSpeed { get; }
    }
}
