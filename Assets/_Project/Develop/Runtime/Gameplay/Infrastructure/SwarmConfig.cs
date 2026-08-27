using UnityEngine;

namespace _Project.Gameplay.Infrastructure
{
    [CreateAssetMenu(fileName = "SwarmConfig", menuName = "Configs/Gameplay/Swarm Config")]
    public sealed class SwarmConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int enemyCount = 500;
        [SerializeField, Min(0f)] private float enemySpeed = 3f;
        [SerializeField, Min(1f)] private float enemyHealth = 100f;
        [SerializeField, Min(1f)] private float targetHealth = 100f;
        [SerializeField, Min(0.01f)] private float enemyColliderRadius = 0.25f;
        [SerializeField, Min(0.01f)] private float targetColliderRadius = 0.35f;
        [SerializeField, Min(0f)] private float innerSpawnRadius = 7f;
        [SerializeField, Min(0f)] private float outerSpawnRadius = 11f;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject playerPrefab;

        public int EnemyCount => enemyCount;
        public float EnemySpeed => enemySpeed;
        public float EnemyHealth => enemyHealth;
        public float TargetHealth => targetHealth;
        public float EnemyColliderRadius => enemyColliderRadius;
        public float TargetColliderRadius => targetColliderRadius;
        public float InnerSpawnRadius => innerSpawnRadius;
        public float OuterSpawnRadius => outerSpawnRadius;
        public int RandomSeed => randomSeed;
        public GameObject EnemyPrefab => enemyPrefab;
        public GameObject PlayerPrefab => playerPrefab;
    }
}