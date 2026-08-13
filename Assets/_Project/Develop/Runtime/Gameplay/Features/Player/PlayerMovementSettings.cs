namespace _Project.Gameplay.Features.Player
{
    public readonly struct PlayerMovementSettings
    {
        public PlayerMovementSettings(float speed)
        {
            Speed = speed;
        }

        public float Speed { get; }
    }
}
