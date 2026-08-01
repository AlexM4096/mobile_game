using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.Collision.Components
{
    public struct CollisionEvent
    {
        public Entity Other;
        public CollisionEventType Type;
        public Vector2 Normal;
        public float Penetration;
    }
}
