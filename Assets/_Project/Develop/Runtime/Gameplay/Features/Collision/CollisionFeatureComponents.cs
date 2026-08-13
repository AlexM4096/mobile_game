using System.Collections.Generic;
using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.Collision
{
    public enum ColliderBodyType
    {
        Static = 0,
        Dynamic = 1
    }

    public enum ColliderKind
    {
        Solid = 0,
        Trigger = 1
    }

    public enum CollisionEventType
    {
        CollisionEnter = 0,
        CollisionStay = 1,
        CollisionExit = 2,
        TriggerEnter = 3,
        TriggerStay = 4,
        TriggerExit = 5
    }

    public enum CollisionLayer
    {
        None = 0,
        Default = 1,
        Player = 2,
        Enemy = 3,
        Environment = 4,
        Projectile = 5
    }

    public struct CircleCollider
    {
        public float Radius;
        public ColliderKind Kind;
        public ColliderBodyType BodyType;
        public CollisionLayer Layer;
    }

    public struct CollisionEvent
    {
        public Entity Other;
        public CollisionEventType Type;
        public Vector2 Normal;
        public float Penetration;
    }

    public struct CollisionEvents { public List<CollisionEvent> Items; }
}
