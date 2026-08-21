using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Gameplay.Features.PooledView
{
    [CreateAssetMenu(
        fileName = "PooledViewCatalog",
        menuName = "_Project/Gameplay/Pooled View Catalog")]
    public sealed partial class PooledViewCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField, Min(1)] private int poolId = 1;
            [SerializeField] private GameObject prefab;
            [SerializeField, Min(0)] private int preloadCount;
            [SerializeField, Min(0)] private int maxRetained;
            [SerializeField, Min(0f)] private float releaseDelay = 0.25f;

            public int PoolId => poolId;
            public GameObject Prefab => prefab;
            public int PreloadCount => preloadCount;
            public int MaxRetained => maxRetained;
            public float ReleaseDelay => releaseDelay;
        }

        [SerializeField, Min(0f)] private float viewportMargin = 0.1f;
        [SerializeField] private List<Entry> entries = new();

        public float ViewportMargin => viewportMargin;
        public IReadOnlyList<Entry> Entries => entries;

        public void ValidateOrThrow()
        {
            if (!TryValidate(out var error))
            {
                throw new InvalidOperationException($"Invalid pooled-view catalog '{name}': {error}");
            }
        }

        private bool TryValidate(out string error)
        {
            if (!IsFiniteNonNegative(viewportMargin))
            {
                error = "Viewport margin must be a finite non-negative value.";
                return false;
            }

            if (entries == null)
            {
                error = "Entries collection is null.";
                return false;
            }

            var ids = new HashSet<int>();
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry == null)
                {
                    error = $"Entry {index} is null.";
                    return false;
                }

                if (entry.PoolId <= 0)
                {
                    error = $"Entry {index} has invalid pool ID {entry.PoolId}; IDs must be positive.";
                    return false;
                }

                if (!ids.Add(entry.PoolId))
                {
                    error = $"Pool ID {entry.PoolId} is duplicated.";
                    return false;
                }

                if (entry.Prefab == null)
                {
                    error = $"Pool ID {entry.PoolId} has no prefab.";
                    return false;
                }

                if (entry.PreloadCount < 0 || entry.MaxRetained < 0)
                {
                    error = $"Pool ID {entry.PoolId} has a negative capacity.";
                    return false;
                }

                if (entry.MaxRetained < entry.PreloadCount)
                {
                    error = $"Pool ID {entry.PoolId} retains fewer views than it preloads.";
                    return false;
                }

                if (!IsFiniteNonNegative(entry.ReleaseDelay))
                {
                    error = $"Pool ID {entry.PoolId} has an invalid release delay.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return value >= 0f && !float.IsInfinity(value) && !float.IsNaN(value);
        }
    }
}
