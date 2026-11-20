using UnityEngine;
using Random = System.Random;

namespace _Project.Gameplay.Features.EnemySpawn
{
    public sealed class SceneSpawnZoneProvider
    {
        private const int MaximumSamplingAttempts = 64;

        public const float ArenaWallThickness = 4f;

        private readonly Collider2D _collider;

        public SceneSpawnZoneProvider(Collider2D collider)
        {
            _collider = collider;
        }

        public bool TryGetLayout(out Bounds spawnBounds, out Rect arena)
        {
            if (_collider == null || !_collider.enabled || !_collider.gameObject.activeInHierarchy)
            {
                spawnBounds = default;
                arena = default;
                return false;
            }

            spawnBounds = _collider.bounds;
            var height = spawnBounds.size.y;
            arena = new Rect(spawnBounds.min.x, spawnBounds.min.y, 2f * height, height);
            return height > 0f && spawnBounds.size.x > 0f;
        }

        public bool TryGeneratePosition(Random random, Bounds bounds, out Vector2 position)
        {
            for (var attempt = 0; attempt < MaximumSamplingAttempts; attempt++)
            {
                var candidate = RandomPointInBounds(random, bounds);
                if (_collider.OverlapPoint(candidate))
                {
                    position = candidate;
                    return true;
                }
            }

            position = _collider.ClosestPoint(bounds.center);
            return _collider.OverlapPoint(position);
        }

        private static Vector2 RandomPointInBounds(Random random, Bounds bounds) =>
            new(
                Mathf.Lerp(bounds.min.x, bounds.max.x, (float)random.NextDouble()),
                Mathf.Lerp(bounds.min.y, bounds.max.y, (float)random.NextDouble()));
    }
}
