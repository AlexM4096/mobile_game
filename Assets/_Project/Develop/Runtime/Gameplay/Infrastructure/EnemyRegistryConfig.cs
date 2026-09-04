using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Gameplay.Infrastructure
{
    [System.Serializable]
    public sealed class EnemyTypeConfig
    {
        [SerializeField, Required] private GameObject prefab;
        [SerializeField, Min(0)] private int enemyCount = 25;
        [SerializeField, Min(0f)] private float baseSpeed = 3f;
        [SerializeField, Min(1f)] private float baseHealth = 100f;
        [SerializeField, Min(0.01f)] private float colliderRadius = 0.25f;
        [SerializeField, Range(0f, 1f)] private float statNoisePercentage = 0.2f;

        public GameObject Prefab => prefab;
        public int EnemyCount => enemyCount;
        public float BaseSpeed => baseSpeed;
        public float BaseHealth => baseHealth;
        public float ColliderRadius => colliderRadius;
        public float StatNoisePercentage => statNoisePercentage;
    }

    [CreateAssetMenu(
        fileName = "EnemyRegistryConfig",
        menuName = "Configs/Gameplay/Enemy Registry Config")]
    public sealed class EnemyRegistryConfig : SerializedScriptableObject
    {
        [SerializeField] private List<EnemyTypeConfig> enemies = new();

        public IReadOnlyList<EnemyTypeConfig> Enemies => enemies;
    }
}
