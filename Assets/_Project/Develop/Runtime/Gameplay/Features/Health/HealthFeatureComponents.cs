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
    public struct DeadTag { }

    public struct DamageOnCollision
    {
        public float Amount;
        public CollisionEventType EventType;
    }
}
