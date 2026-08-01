namespace _Project.Gameplay.Features.Movement
{
    public sealed class MovementSettings
    {
        public MovementSettings(float arrivalDistance)
        {
            ArrivalDistance = arrivalDistance;
        }

        public float ArrivalDistance { get; }
    }
}
