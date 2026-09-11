using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Gameplay.Infrastructure
{
    [CreateAssetMenu(fileName = "SwarmConfig", menuName = "Configs/Gameplay/Swarm Config")]
    public sealed class SwarmConfig : SerializedScriptableObject
    {
        [SerializeField, Min(1f)] private float targetHealth = 100f;
        [SerializeField, Min(0.01f)] private float targetColliderRadius = 0.35f;
        [SerializeField, Required] private GameObject playerPrefab;

        public float TargetHealth => targetHealth;
        public float TargetColliderRadius => targetColliderRadius;
        public GameObject PlayerPrefab => playerPrefab;
    }
}
