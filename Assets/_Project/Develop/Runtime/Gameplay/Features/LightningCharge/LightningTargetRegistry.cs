using System.Collections.Generic;
using Arch.Core;
using UnityEngine;

namespace _Project.Gameplay.Features.LightningCharge
{
    public sealed class LightningTargetRegistry
    {
        private const float DistanceTieEpsilon = 0.000001f;

        private readonly List<TargetSnapshot> _targets = new();
        private readonly Dictionary<Entity, Vector2> _positions = new();

        public void Clear()
        {
            _targets.Clear();
            _positions.Clear();
        }

        public void Add(Entity entity, Vector2 position)
        {
            _targets.Add(new TargetSnapshot(entity, position));
            _positions[entity] = position;
        }

        public bool TryGetPosition(Entity entity, out Vector2 position) =>
            _positions.TryGetValue(entity, out position);

        public bool TryGetNearest(
            Vector2 origin,
            float radius,
            HashSet<Entity> excluded,
            out Entity entity,
            out Vector2 position)
        {
            var radiusSquared = radius * radius;
            var bestDistanceSquared = float.MaxValue;
            entity = default;
            position = default;
            var found = false;

            for (var index = 0; index < _targets.Count; index++)
            {
                var target = _targets[index];
                if (excluded.Contains(target.Entity))
                {
                    continue;
                }

                var distanceSquared = (target.Position - origin).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                {
                    continue;
                }

                if (found &&
                    distanceSquared > bestDistanceSquared - DistanceTieEpsilon)
                {
                    if (Mathf.Abs(distanceSquared - bestDistanceSquared) > DistanceTieEpsilon ||
                        target.Entity.CompareTo(entity) >= 0)
                    {
                        continue;
                    }
                }

                found = true;
                bestDistanceSquared = distanceSquared;
                entity = target.Entity;
                position = target.Position;
            }

            return found;
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
