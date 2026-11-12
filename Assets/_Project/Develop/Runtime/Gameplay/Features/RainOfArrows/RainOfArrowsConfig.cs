using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.Features.RainOfArrows
{
    [CreateAssetMenu(
        fileName = "RainOfArrowsConfig",
        menuName = "Configs/Gameplay/Rain Of Arrows Config")]
    public sealed class RainOfArrowsConfig : ScriptableObject
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private List<Sprite> projectileSprites = new();
        [SerializeField, Min(0.01f)] private float cooldownDuration = 8f;
        [SerializeField, Min(0.01f)] private float duration = 4f;
        [SerializeField, Min(1)] private int maxConcurrentZones = 1;
        [SerializeField, Min(0.01f)] private float targetingRangeInTiles = 6f;
        [SerializeField, Min(0.01f)] private float zoneRadiusInTiles = 3f;
        [SerializeField, Min(0.01f)] private float impactRadiusInTiles = 0.5f;
        [SerializeField, Min(1)] private int projectileCount = 12;
        [SerializeField, Min(1)] private int damageHits = 1;
        [SerializeField, Min(0.01f)] private float projectileSpeed = 12f;
        [SerializeField, Min(0.01f)] private float fallHeightInTiles = 2f;
        [SerializeField] private Color outlineColor = new(0.65f, 0.2f, 1f, 0.9f);
        [SerializeField, Min(0.001f)] private float outlineWidth = 0.08f;
        [SerializeField, Min(0.01f)] private float dashLength = 0.35f;

        public GameObject ProjectilePrefab => projectilePrefab;
        public IReadOnlyList<Sprite> ProjectileSprites => projectileSprites;
        public float CooldownDuration => cooldownDuration;
        public float Duration => duration;
        public int MaxConcurrentZones => maxConcurrentZones;
        public float TargetingRangeInTiles => targetingRangeInTiles;
        public float ZoneRadiusInTiles => zoneRadiusInTiles;
        public float ImpactRadiusInTiles => impactRadiusInTiles;
        public int ProjectileCount => projectileCount;
        public int DamageHits => damageHits;
        public float ProjectileSpeed => projectileSpeed;
        public float FallHeightInTiles => fallHeightInTiles;
        public Color OutlineColor => outlineColor;
        public float OutlineWidth => outlineWidth;
        public float DashLength => dashLength;

        public float GetTileSize(Grid environmentGrid)
        {
            if (environmentGrid == null)
            {
                return 0f;
            }

            var cellSize = environmentGrid.cellSize;
            return Mathf.Min(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
        }

        public float GetTargetingRange(Grid environmentGrid) =>
            GetTileSize(environmentGrid) * targetingRangeInTiles;

        public float GetZoneRadius(Grid environmentGrid) =>
            GetTileSize(environmentGrid) * zoneRadiusInTiles;

        public float GetImpactRadius(Grid environmentGrid) =>
            GetTileSize(environmentGrid) * impactRadiusInTiles;

        public float GetFallHeight(Grid environmentGrid) =>
            GetTileSize(environmentGrid) * fallHeightInTiles;

        internal bool TryValidate(Grid environmentGrid, out string error)
        {
            if (environmentGrid == null || GetTileSize(environmentGrid) <= 0f)
            {
                error = "Rain of Arrows requires a gameplay Grid with positive cell dimensions.";
                return false;
            }

            if (projectilePrefab == null)
            {
                error = "Rain of Arrows requires a projectile prefab.";
                return false;
            }

            if (!projectilePrefab.TryGetComponent<RainOfArrowsView>(out _))
            {
                error = "Rain of Arrows projectile prefab requires a RainOfArrowsView component.";
                return false;
            }

            if (projectileSprites == null || projectileSprites.Count == 0)
            {
                error = "Rain of Arrows requires at least one projectile sprite.";
                return false;
            }

            for (var index = 0; index < projectileSprites.Count; index++)
            {
                if (projectileSprites[index] == null)
                {
                    error = "Rain of Arrows projectile sprites cannot contain null entries.";
                    return false;
                }
            }

            var fallTime = GetFallHeight(environmentGrid) / projectileSpeed;
            if (cooldownDuration <= 0f ||
                duration <= 0f ||
                maxConcurrentZones < 1 ||
                targetingRangeInTiles <= 0f ||
                zoneRadiusInTiles <= 0f ||
                impactRadiusInTiles <= 0f ||
                projectileCount < 1 ||
                damageHits < 1 ||
                projectileSpeed <= 0f ||
                fallHeightInTiles <= 0f ||
                fallTime > duration ||
                outlineWidth <= 0f ||
                dashLength <= 0f)
            {
                error = "Rain of Arrows has invalid numeric settings.";
                return false;
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            cooldownDuration = Mathf.Max(0.01f, cooldownDuration);
            duration = Mathf.Max(0.01f, duration);
            maxConcurrentZones = Mathf.Max(1, maxConcurrentZones);
            targetingRangeInTiles = Mathf.Max(0.01f, targetingRangeInTiles);
            zoneRadiusInTiles = Mathf.Max(0.01f, zoneRadiusInTiles);
            impactRadiusInTiles = Mathf.Max(0.01f, impactRadiusInTiles);
            projectileCount = Mathf.Max(1, projectileCount);
            damageHits = Mathf.Max(1, damageHits);
            projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
            fallHeightInTiles = Mathf.Max(0.01f, fallHeightInTiles);
            outlineWidth = Mathf.Max(0.001f, outlineWidth);
            dashLength = Mathf.Max(0.01f, dashLength);
        }
    }
}
