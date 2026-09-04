using UnityEngine;

namespace _Project.Gameplay.Features.Sword
{
    public enum SwordRotationDirection
    {
        Clockwise = -1,
        CounterClockwise = 1
    }

    [CreateAssetMenu(fileName = "SwordConfig", menuName = "Configs/Gameplay/Sword Config")]
    public sealed class SwordConfig : ScriptableObject
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0.01f)] private float rotationSpeedDegreesPerSecond = 180f;
        [SerializeField, Min(0.01f)] private float rotationRadius = 2f;
        [SerializeField] private SwordRotationDirection direction = SwordRotationDirection.Clockwise;
        [SerializeField, Min(1)] private int weaponCount = 1;
        [SerializeField, Min(0.01f)] private float damage = 25f;
        [SerializeField] private Vector2 hitboxSize = new(2f, 1.3f);

        public GameObject Prefab => prefab;
        public float RotationSpeedDegreesPerSecond => rotationSpeedDegreesPerSecond;
        public float RotationRadius => rotationRadius;
        public SwordRotationDirection Direction => direction;
        public int WeaponCount => weaponCount;
        public float Damage => damage;
        public Vector2 HitboxSize => hitboxSize;

        private void OnValidate()
        {
            rotationSpeedDegreesPerSecond = Mathf.Max(0.01f, rotationSpeedDegreesPerSecond);
            rotationRadius = Mathf.Max(0.01f, rotationRadius);
            weaponCount = Mathf.Max(1, weaponCount);
            damage = Mathf.Max(0.01f, damage);
            hitboxSize.x = Mathf.Max(0.01f, hitboxSize.x);
            hitboxSize.y = Mathf.Max(0.01f, hitboxSize.y);

            if (direction != SwordRotationDirection.Clockwise &&
                direction != SwordRotationDirection.CounterClockwise)
            {
                direction = SwordRotationDirection.Clockwise;
            }
        }
    }
}
