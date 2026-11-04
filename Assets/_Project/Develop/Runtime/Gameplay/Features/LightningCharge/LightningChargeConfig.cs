using UnityEngine;

namespace _Project.Gameplay.Features.LightningCharge
{
    [CreateAssetMenu(
        fileName = "LightningChargeConfig",
        menuName = "Configs/Gameplay/Lightning Charge Config")]
    public sealed class LightningChargeConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float cooldownDuration = 6f;
        [SerializeField, Min(0.01f)] private float chargeDuration = 5f;
        [SerializeField, Min(1)] private int baseChargeCount = 1;
        [SerializeField, Min(0.01f)] private float transferRadiusInTiles = 3f;
        [SerializeField, Min(1)] private int damageHits = 1;
        [SerializeField, Min(1)] private int maxTargetsPerChain = 3;
        [SerializeField] private Color chargedColor = new(0.2f, 0.9f, 1f, 1f);
        [SerializeField] private Color arcColor = new(0.2f, 0.9f, 1f, 1f);
        [SerializeField, Min(0.01f)] private float arcWidth = 0.08f;
        [SerializeField, Min(0.01f)] private float arcLifetime = 0.15f;

        public float CooldownDuration => cooldownDuration;
        public float ChargeDuration => chargeDuration;
        public int BaseChargeCount => baseChargeCount;
        public float TransferRadiusInTiles => transferRadiusInTiles;
        public int DamageHits => damageHits;
        public int MaxTargetsPerChain => maxTargetsPerChain;
        public Color ChargedColor => chargedColor;
        public Color ArcColor => arcColor;
        public float ArcWidth => arcWidth;
        public float ArcLifetime => arcLifetime;

        public float GetTransferRadius(Grid environmentGrid)
        {
            if (environmentGrid == null)
            {
                return 0f;
            }

            var cellSize = environmentGrid.cellSize;
            var tileSize = Mathf.Min(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
            return tileSize * transferRadiusInTiles;
        }

        internal bool TryValidate(Grid environmentGrid, out string error)
        {
            if (environmentGrid == null ||
                Mathf.Abs(environmentGrid.cellSize.x) <= 0f ||
                Mathf.Abs(environmentGrid.cellSize.y) <= 0f)
            {
                error = "Lightning Charge requires a gameplay Grid with positive cell dimensions.";
                return false;
            }

            if (cooldownDuration <= 0f ||
                chargeDuration <= 0f ||
                baseChargeCount < 1 ||
                transferRadiusInTiles <= 0f ||
                damageHits < 1 ||
                maxTargetsPerChain < 1 ||
                arcWidth <= 0f ||
                arcLifetime <= 0f)
            {
                error = "Lightning Charge has invalid numeric settings.";
                return false;
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            cooldownDuration = Mathf.Max(0.01f, cooldownDuration);
            chargeDuration = Mathf.Max(0.01f, chargeDuration);
            baseChargeCount = Mathf.Max(1, baseChargeCount);
            transferRadiusInTiles = Mathf.Max(0.01f, transferRadiusInTiles);
            damageHits = Mathf.Max(1, damageHits);
            maxTargetsPerChain = Mathf.Max(1, maxTargetsPerChain);
            arcWidth = Mathf.Max(0.01f, arcWidth);
            arcLifetime = Mathf.Max(0.01f, arcLifetime);
        }
    }
}
