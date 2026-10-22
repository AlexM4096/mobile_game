using Sirenix.OdinInspector;
using _Project.Gameplay.Features.Health;
using UnityEngine;

namespace _Project.Gameplay.Features.EnemySpawn
{
    [CreateAssetMenu(
        fileName = "EnemyEquipmentVisualConfig",
        menuName = "Configs/Gameplay/Enemy Equipment Visuals")]
    public sealed class EnemyEquipmentVisualConfig : SerializedScriptableObject
    {
        [SerializeField, Required] private Sprite staffSprite;
        [SerializeField, Required] private Sprite daggerSprite;
        [SerializeField, Required] private Sprite axeSprite;
        [SerializeField, Required] private Sprite clothHelmetSprite;
        [SerializeField, Required] private Sprite woodHelmetSprite;
        [SerializeField, Required] private Sprite ironHelmetSprite;

        public Sprite GetWeaponSprite(WeaponType weaponType) => weaponType switch
        {
            WeaponType.None => null,
            WeaponType.Staff => staffSprite,
            WeaponType.Dagger => daggerSprite,
            WeaponType.Axe => axeSprite,
            _ => null
        };

        public Sprite GetHelmetSprite(HelmetType helmetType) => helmetType switch
        {
            HelmetType.None => null,
            HelmetType.Cloth => clothHelmetSprite,
            HelmetType.Wood => woodHelmetSprite,
            HelmetType.Iron => ironHelmetSprite,
            HelmetType.DragonScale => ironHelmetSprite,
            _ => null
        };

        public bool TryValidate(out string error)
        {
            if (staffSprite == null ||
                daggerSprite == null ||
                axeSprite == null ||
                clothHelmetSprite == null ||
                woodHelmetSprite == null ||
                ironHelmetSprite == null)
            {
                error = "Enemy equipment visuals require every weapon and helmet sprite binding.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
