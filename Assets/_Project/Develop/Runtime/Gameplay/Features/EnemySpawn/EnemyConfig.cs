using _Project.Gameplay.Features.Health;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Gameplay.Features.EnemySpawn
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Gameplay/Enemy")]
    public sealed class EnemyConfig : SerializedScriptableObject
    {
        [SerializeField, Required] private GameObject prefab;
        [SerializeField, Min(0f)] private float baseSpeed = 3f;
        [SerializeField, Min(0.01f)] private float colliderRadius = 0.25f;
        [SerializeField, Range(0f, 1f)] private float statNoisePercentage = 0.2f;
        [SerializeField, Min(1)] private int bodyDurability = 1;
        [SerializeField, Min(1)] private int experienceReward = 1;
        [SerializeField, InlineEditor] private EnemyConfig splitChild;
        [SerializeField, Min(0), ShowIf(nameof(HasSplitChild))] private int splitCount;
        [SerializeField, Min(0.01f), ShowIf(nameof(HasActiveSplit))] private float splitDelay = 0.25f;
        [SerializeField, ShowIf(nameof(HasActiveSplit))] private WeaponType splitWeaponType;
        [SerializeField, ShowIf(nameof(HasActiveSplit))] private HelmetType splitHelmetType;
        [SerializeField, ShowIf(nameof(HasSplitHelmet))] private bool splitEnchantedArmor;

        public GameObject Prefab => prefab;
        public float BaseSpeed => baseSpeed;
        public float ColliderRadius => colliderRadius;
        public float StatNoisePercentage => statNoisePercentage;
        public int BodyDurability => bodyDurability;
        public int ExperienceReward => experienceReward;
        public EnemyConfig SplitChild => splitChild;
        public int SplitCount => splitCount;
        public float SplitDelay => splitDelay;
        public EnemyEquipmentLoadout SplitLoadout =>
            new(splitWeaponType, splitHelmetType, splitEnchantedArmor);

        private bool HasSplitChild => splitChild != null;
        private bool HasActiveSplit => HasSplitChild && splitCount > 0;
        private bool HasSplitHelmet => HasActiveSplit && splitHelmetType != HelmetType.None;
    }
}
