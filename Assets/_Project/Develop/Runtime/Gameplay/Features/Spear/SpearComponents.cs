using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.Spear
{
    public struct SpearAbilityCooldown { public double NextActivationTime; }

    public struct SpearShotRequest
    {
        public Entity Owner;
        public Vector2 Direction;
        public double FireTime;
    }

    public struct SpearProjectile
    {
        public int DamageHits;
        public int RemainingDurability;
    }
}
