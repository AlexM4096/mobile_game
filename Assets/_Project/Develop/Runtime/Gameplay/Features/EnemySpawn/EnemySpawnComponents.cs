using _Project.Gameplay.Features.Health;

namespace _Project.Gameplay.Features.EnemySpawn
{
    public struct SplitOnDeath
    {
        public int ChildConfigId;
        public int Count;
        public float Delay;
        public double SpawnAt;
        public bool AnimationStarted;
        public WeaponType WeaponType;
        public HelmetType HelmetType;
        public bool IsEnchanted;
    }

    public struct SplitProcessedTag { }
}
