using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Gameplay.Infrastructure
{
    public enum SpawnShape
    {
        Circle,
        Box
    }

    [CreateAssetMenu(fileName = "SwarmConfig", menuName = "Configs/Gameplay/Swarm Config")]
    public sealed class SwarmConfig : SerializedScriptableObject
    {
        [SerializeField, Required] private EnemyRegistryConfig enemyRegistry;
        [SerializeField] private SpawnShape spawnShape = SpawnShape.Circle;
        [SerializeField, ToggleLeft] private bool spawnOnEdge;
        [SerializeField, Min(0.01f), ShowIf(nameof(spawnShape), SpawnShape.Circle)]
        private float circleRadius = 11f;
        [SerializeField, Min(0.01f), ShowIf(nameof(spawnShape), SpawnShape.Box)]
        private float boxWidth = 22f;
        [SerializeField, Min(0.01f), ShowIf(nameof(spawnShape), SpawnShape.Box)]
        private float boxHeight = 22f;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField, Min(1f)] private float targetHealth = 100f;
        [SerializeField, Min(0.01f)] private float targetColliderRadius = 0.35f;
        [SerializeField, Required] private GameObject playerPrefab;

        public EnemyRegistryConfig EnemyRegistry => enemyRegistry;
        public SpawnShape SpawnShape => spawnShape;
        public bool SpawnOnEdge => spawnOnEdge;
        public float CircleRadius => circleRadius;
        public float BoxWidth => boxWidth;
        public float BoxHeight => boxHeight;
        public int RandomSeed => randomSeed;
        public float TargetHealth => targetHealth;
        public float TargetColliderRadius => targetColliderRadius;
        public GameObject PlayerPrefab => playerPrefab;
    }
}
