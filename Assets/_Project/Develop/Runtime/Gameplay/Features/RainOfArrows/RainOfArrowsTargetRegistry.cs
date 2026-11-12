using System.Collections.Generic;
using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.RainOfArrows
{
    public sealed class RainOfArrowsTargetRegistry
    {
        private const float ComparisonEpsilon = 0.000001f;
        private readonly List<TargetSnapshot> _targets = new();

        public void Clear() => _targets.Clear();

        public void Add(Entity entity, Vector2 position) =>
            _targets.Add(new TargetSnapshot(entity, position));

        public bool TryFindBestZoneCenter(
            Vector2 playerPosition,
            float targetingRange,
            float zoneRadius,
            out Vector2 center)
        {
            center = default;
            var found = false;
            var bestScore = -1;
            var bestDistanceSquared = float.MaxValue;
            var targetingRangeSquared = targetingRange * targetingRange;
            var zoneRadiusSquared = zoneRadius * zoneRadius;

            for (var candidateIndex = 0; candidateIndex < _targets.Count; candidateIndex++)
            {
                var candidate = _targets[candidateIndex].Position;
                var distanceSquared = (candidate - playerPosition).sqrMagnitude;
                if (distanceSquared > targetingRangeSquared)
                {
                    continue;
                }

                var score = 0;
                for (var targetIndex = 0; targetIndex < _targets.Count; targetIndex++)
                {
                    if ((_targets[targetIndex].Position - candidate).sqrMagnitude <=
                        zoneRadiusSquared)
                    {
                        score++;
                    }
                }

                if (!IsBetterCandidate(
                        score,
                        distanceSquared,
                        candidate,
                        bestScore,
                        bestDistanceSquared,
                        center))
                {
                    continue;
                }

                found = true;
                bestScore = score;
                bestDistanceSquared = distanceSquared;
                center = candidate;
            }

            return found;
        }

        public void AccumulateHits(
            Vector2 impactPosition,
            float impactRadius,
            int damageHits,
            Dictionary<Entity, int> hitsByEnemy)
        {
            var radiusSquared = impactRadius * impactRadius;
            for (var index = 0; index < _targets.Count; index++)
            {
                var target = _targets[index];
                if ((target.Position - impactPosition).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                hitsByEnemy.TryGetValue(target.Entity, out var existingHits);
                hitsByEnemy[target.Entity] = existingHits + damageHits;
            }
        }

        private static bool IsBetterCandidate(
            int score,
            float distanceSquared,
            Vector2 candidate,
            int bestScore,
            float bestDistanceSquared,
            Vector2 bestCandidate)
        {
            if (score != bestScore)
            {
                return score > bestScore;
            }

            if (Mathf.Abs(distanceSquared - bestDistanceSquared) > ComparisonEpsilon)
            {
                return distanceSquared < bestDistanceSquared;
            }

            if (Mathf.Abs(candidate.x - bestCandidate.x) > ComparisonEpsilon)
            {
                return candidate.x < bestCandidate.x;
            }

            return candidate.y < bestCandidate.y;
        }

        private readonly struct TargetSnapshot
        {
            public TargetSnapshot(Entity entity, Vector2 position)
            {
                Entity = entity;
                Position = position;
            }

            public Entity Entity { get; }
            public Vector2 Position { get; }
        }
    }
}
