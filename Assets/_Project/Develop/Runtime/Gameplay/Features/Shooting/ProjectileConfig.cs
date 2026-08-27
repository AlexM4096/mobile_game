using UnityEngine;

namespace _Project.Gameplay.Features.Shooting
{
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "Configs/Gameplay/Projectile Config")]
    public sealed class ProjectileConfig : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private float speed;
        [SerializeField] private float damage;
        [SerializeField] private float radius;
        [SerializeField] private float lifetime;
        [SerializeField] private float fireCooldown;

        public GameObject Prefab => prefab;
        public float Speed => speed;
        public float Damage => damage;
        public float Radius => radius;
        public float Lifetime => lifetime;
        public float FireCooldown => fireCooldown;
    }
}