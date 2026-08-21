namespace _Project.Gameplay.Features.Shooting
{
    public readonly struct ProjectileSettings
    {
        public ProjectileSettings(
            int viewPoolId,
            float speed,
            float damage,
            float radius,
            float lifetime,
            float fireCooldown)
        {
            ViewPoolId = viewPoolId;
            Speed = speed;
            Damage = damage;
            Radius = radius;
            Lifetime = lifetime;
            FireCooldown = fireCooldown;
        }

        public int ViewPoolId { get; }
        public float Speed { get; }
        public float Damage { get; }
        public float Radius { get; }
        public float Lifetime { get; }
        public float FireCooldown { get; }
    }
}
