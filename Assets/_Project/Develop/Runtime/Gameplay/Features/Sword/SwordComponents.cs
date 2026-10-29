using Arch.Core;

namespace _Project.Gameplay.Features.Sword
{
    public struct SwordTag { }

    public struct SwordAbility
    {
        public int WeaponCount;
        public float BaseRotationSpeedDegreesPerSecond;
        public float RotationSpeedMultiplier;
        public float OrbitPhaseRadians;
    }

    public struct SwordOrbit
    {
        public Entity Owner;
        public int SlotIndex;
    }
}
