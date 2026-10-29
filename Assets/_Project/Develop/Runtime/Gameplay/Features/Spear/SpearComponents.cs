using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.Spear
{
    public struct SpearAbility
    {
        public double NextActivationTime;
        public float CooldownDuration;
        public int ProjectileCount;
        public int DamageHits;
        public int TotalHealth;
    }

    public struct SpearShotRequest
    {
        public Entity Owner;
        public Vector2 Direction;
        public double FireTime;
        public int DamageHits;
        public int TotalHealth;
    }

    public struct SpearProjectile
    {
        public int DamageHits;
        public int RemainingDurability;
    }
}
