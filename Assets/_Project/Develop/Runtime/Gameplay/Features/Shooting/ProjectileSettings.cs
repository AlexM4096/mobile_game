using UnityEngine;

namespace _Project.Gameplay.Features.Shooting
{
    public readonly struct ProjectileSettings
    {
        public ProjectileSettings(
            GameObject prefab,
            float speed,
            float damage,
            float radius,
            float lifetime,
            float fireCooldown)
        {
            Prefab = prefab;
            Speed = speed;
            Damage = damage;
            Radius = radius;
            Lifetime = lifetime;
            FireCooldown = fireCooldown;
        }

        public GameObject Prefab { get; }
        public float Speed { get; }
        public float Damage { get; }
        public float Radius { get; }
        public float Lifetime { get; }
        public float FireCooldown { get; }
    }
}
