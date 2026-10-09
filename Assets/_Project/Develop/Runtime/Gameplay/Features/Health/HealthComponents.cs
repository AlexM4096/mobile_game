using System;
using _Project.Gameplay.Features.Collision;

namespace _Project.Gameplay.Features.Health
{
    public struct Health
    {
        public float Current;
        public float Max;
    }

    public struct DamageRequest { public float Amount; }
    public struct HealRequest { public float Amount; }
    public struct HitRequest { public int Count; }

    public struct DamageOnCollision
    {
        public float Amount;
        public CollisionPhase Phase;
    }

    public enum HelmetType
    {
        None,
        Cloth,
        Wood,
        Iron,
        DragonScale
    }

    public enum HelmetVisualState
    {
        None,
        FullDurability,
        Damaged,
        OneHitRemaining
    }

    [Flags]
    public enum BrokenDefenseLayers
    {
        None = 0,
        Helmet = 1 << 0,
        Weapon = 1 << 1,
        Body = 1 << 2
    }

    public struct EnemyDefense
    {
        public HelmetType HelmetType;
        public int HelmetDurability;
        public int MaxHelmetDurability;
        public bool HasWeapon;
        public bool IsEnchanted;
    }

    public struct DefenseViewUpdate
    {
        public int HelmetDurability;
        public BrokenDefenseLayers BrokenLayers;
    }
}