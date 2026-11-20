using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementConfig", menuName = "Configs/Gameplay/Player Movement Config")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float tangentialSpeed = 6f;
        [SerializeField, Min(0f)] private float launchSpeed = 8f;
        [SerializeField, Min(0.05f)] private float orbitChargeDuration = 1.2f;
        [SerializeField, Min(1f)] private float fullChargeOrbitMultiplier = 2f;
        [SerializeField, Min(1f)] private float fullChargeLaunchMultiplier = 1.8f;
        [SerializeField, Min(0f)] private float launchBurstDuration = 0.3f;
        [SerializeField, Min(0f)] private float launchBurstSpeedBonus = 8f;
        [SerializeField, Min(1)] private int fireComboThreshold = 20;
        [SerializeField, Min(1f)] private float fireComboSpeedMultiplier = 1.12f;
        [SerializeField, Min(1f)] private float fireComboChargeMultiplier = 1.25f;
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

        public float OrbitChargeDuration => Mathf.Max(0.05f, orbitChargeDuration);
        public float LaunchBurstDuration => Mathf.Max(0f, launchBurstDuration);
        public int FireComboThreshold => Mathf.Max(1, fireComboThreshold);
        public float FireComboChargeMultiplier => Mathf.Max(1f, fireComboChargeMultiplier);

        public float GetSpeedMultiplier(bool comboFireActive) =>
            comboFireActive ? Mathf.Max(1f, fireComboSpeedMultiplier) : 1f;

        public float GetOrbitSpeed(float charge)
        {
            return Mathf.Lerp(tangentialSpeed, tangentialSpeed * fullChargeOrbitMultiplier, charge);
        }

        public float GetLaunchSpeed(float charge)
        {
            var chargedSpeed = Mathf.Lerp(launchSpeed, launchSpeed * fullChargeLaunchMultiplier, charge);
            return Mathf.Min(chargedSpeed, maximumSpeed);
        }

        public float GetLaunchBurstBonus(float remaining)
        {
            return launchBurstDuration > 0f
                ? launchBurstSpeedBonus * Mathf.Clamp01(remaining / launchBurstDuration)
                : 0f;
        }
    }
}
