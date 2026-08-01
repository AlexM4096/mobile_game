namespace _Project.Gameplay.Features.Health.Components
{
    public struct Health
    {
        public float Current;
        public float Max;

        public float Normalized => Max > 0f ? Current / Max : 0f;
    }
}
