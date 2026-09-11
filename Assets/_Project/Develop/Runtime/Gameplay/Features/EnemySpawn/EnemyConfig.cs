using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Gameplay.Features.EnemySpawn
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Gameplay/Enemy")]
    public sealed class EnemyConfig : SerializedScriptableObject
    {
        [SerializeField, Required] private GameObject prefab;
        [SerializeField, Min(0f)] private float baseSpeed = 3f;
        [SerializeField, Min(1f)] private float baseHealth = 100f;
        [SerializeField, Min(0.01f)] private float colliderRadius = 0.25f;
        [SerializeField, Range(0f, 1f)] private float statNoisePercentage = 0.2f;

        public GameObject Prefab => prefab;
        public float BaseSpeed => baseSpeed;
        public float BaseHealth => baseHealth;
        public float ColliderRadius => colliderRadius;
        public float StatNoisePercentage => statNoisePercentage;
    }
}
