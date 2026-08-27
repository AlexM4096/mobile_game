using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    public struct PlayerTag { }

    public enum PlayerMotionMode
    {
        Flight = 0,
        Orbit = 1
    }

    public struct PlayerMotion
    {
        public PlayerMotionMode Mode;
        public Vector2 OrbitCenter;
        public float OrbitRadius;
        public float OrbitDirection;
    }
}
