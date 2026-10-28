using UnityEngine;

namespace _Project.Gameplay.Features.Spear
{
    [CreateAssetMenu(fileName = "SpearConfig", menuName = "Configs/Gameplay/Spear Config")]
    public sealed class SpearConfig : ScriptableObject
    {
        [SerializeField] private bool enabled = true;
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0.01f)] private float cooldownDuration = 2f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 5f;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Min(0.01f)] private float triggerRadius = 10f;
        [SerializeField, Min(0f)] private float targetingDelay = 0.25f;
        [SerializeField, Min(0.01f)] private float thicknessMultiplier = 0.35f;
        [SerializeField, Min(1)] private int damageHits = 1;
        [SerializeField, Min(0.01f)] private float speed = 15f;
        [SerializeField, Min(1)] private int totalHealth = 3;

        public bool Enabled => enabled;
        public GameObject Prefab => prefab;
        public float CooldownDuration => cooldownDuration;
        public float ProjectileLifetime => projectileLifetime;
        public int ProjectileCount => projectileCount;
        public float TriggerRadius => triggerRadius;
        public float TargetingDelay => targetingDelay;
        public float ThicknessMultiplier => thicknessMultiplier;
        public int DamageHits => damageHits;
        public float Speed => speed;
        public int TotalHealth => totalHealth;

        internal bool TryValidate(out string error)
        {
            if (!enabled)
            {
                error = null;
                return true;
            }

            if (prefab == null)
            {
                error = "Spear requires a projectile prefab while the ability is enabled.";
                return false;
            }

            if (!prefab.TryGetComponent<SpearView>(out _))
            {
                error = "Spear projectile prefab requires a SpearView component.";
                return false;
            }

            if (cooldownDuration <= 0f ||
                projectileLifetime <= 0f ||
                projectileCount < 1 ||
                triggerRadius <= 0f ||
                targetingDelay < 0f ||
                thicknessMultiplier <= 0f ||
                damageHits < 1 ||
                speed <= 0f ||
                totalHealth < 1)
            {
                error = "Spear has invalid numeric settings.";
                return false;
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            cooldownDuration = Mathf.Max(0.01f, cooldownDuration);
            projectileLifetime = Mathf.Max(0.01f, projectileLifetime);
            projectileCount = Mathf.Max(1, projectileCount);
            triggerRadius = Mathf.Max(0.01f, triggerRadius);
            targetingDelay = Mathf.Max(0f, targetingDelay);
            thicknessMultiplier = Mathf.Max(0.01f, thicknessMultiplier);
            damageHits = Mathf.Max(1, damageHits);
            speed = Mathf.Max(0.01f, speed);
            totalHealth = Mathf.Max(1, totalHealth);
        }
    }
}
