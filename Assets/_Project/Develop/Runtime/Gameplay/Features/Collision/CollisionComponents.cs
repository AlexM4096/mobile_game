using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.Collision
{
    public enum ColliderBodyType
    {
        Static = 0,
        Dynamic = 1
    }

    public enum CollisionPhase
    {
        Enter = 0,
        Stay = 1,
        Exit = 2
    }

    public enum CollisionLayer
    {
        None = 0,
        Default = 1,
        Player = 2,
        Enemy = 3,
        Environment = 4,
        Projectile = 5,
        PlayerWeapon = 6
    }

    public struct CollisionBody
    {
        public ColliderBodyType BodyType;
        public CollisionLayer Layer;
    }

    public struct CircleCollider { public float Radius; }

    public struct BoxCollider { public Vector2 HalfExtents; }

    public struct TriggerTag { }

    public struct CollisionEvent
    {
        public Entity First;
        public Entity Second;
        public CollisionPhase Phase;
        public Vector2 Point;
        public Vector2 Normal;
        public float Penetration;
    }
}
