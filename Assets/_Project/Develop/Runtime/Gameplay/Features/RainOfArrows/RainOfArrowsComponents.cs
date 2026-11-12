using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.RainOfArrows
{
    public struct RainOfArrowsAbility
    {
        public double NextActivationTime;
        public float CooldownDuration;
        public float Duration;
        public int MaxConcurrentZones;
        public int ActiveZones;
        public float TargetingRange;
        public float ZoneRadius;
        public float ImpactRadius;
        public int ProjectileCount;
        public int DamageHits;
        public float ProjectileSpeed;
    }

    public struct RainOfArrowsZone
    {
        public Entity Owner;
        public double EndTime;
        public double NextLaunchTime;
        public float LaunchInterval;
        public float ZoneRadius;
        public float ImpactRadius;
        public float ProjectileSpeed;
        public float FallHeight;
        public int ProjectileCount;
        public int LaunchedProjectiles;
        public int DamageHits;
    }

    public struct RainOfArrowsProjectile
    {
        public Vector2 ImpactPoint;
        public float ImpactRadius;
        public float Speed;
        public int DamageHits;
        public double ImpactTime;
    }

    public struct RainOfArrowsImpactRequest
    {
        public Vector2 Position;
        public float Radius;
        public int DamageHits;
    }
}
