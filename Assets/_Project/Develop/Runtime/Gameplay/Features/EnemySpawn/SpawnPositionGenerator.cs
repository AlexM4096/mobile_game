using System;
using UnityEngine;
using Random = System.Random;

namespace _Project.Gameplay.Features.EnemySpawn
{
    internal static class SpawnPositionGenerator
    {
        public static Vector2 GeneratePosition(Random random, SpawnZone zone)
        {
            var localPosition = (zone.Shape, zone.SpawnOnEdge) switch
            {
                (SpawnShape.Circle, false) => InsideCircle(random, zone.CircleRadius),
                (SpawnShape.Circle, true) => OnCircle(random, zone.CircleRadius),
                (SpawnShape.Box, false) => InsideBox(random, zone.BoxSize),
                (SpawnShape.Box, true) => OnBox(random, zone.BoxSize),
                _ => throw new ArgumentOutOfRangeException()
            };
            return zone.Center + localPosition;
        }

        public static float ApplyNoise(Random random, float baseValue, float noisePercentage)
        {
            var noise = Mathf.Clamp01(noisePercentage);
            var multiplier = Mathf.Lerp(1f - noise, 1f + noise, (float)random.NextDouble());
            return baseValue * multiplier;
        }

        private static Vector2 InsideCircle(Random random, float radius)
        {
            var angle = Angle(random);
            var distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
            return Direction(angle) * distance;
        }

        private static Vector2 OnCircle(Random random, float radius)
        {
            return Direction(Angle(random)) * radius;
        }

        private static Vector2 InsideBox(Random random, Vector2 size)
        {
            return new Vector2(
                Range(random, -size.x * 0.5f, size.x * 0.5f),
                Range(random, -size.y * 0.5f, size.y * 0.5f));
        }

        private static Vector2 OnBox(Random random, Vector2 size)
        {
            var halfWidth = size.x * 0.5f;
            var halfHeight = size.y * 0.5f;
            var distance = (float)random.NextDouble() * (2f * (size.x + size.y));
            if (distance < size.x) return new Vector2(-halfWidth + distance, halfHeight);

            distance -= size.x;
            if (distance < size.y) return new Vector2(halfWidth, halfHeight - distance);

            distance -= size.y;
            if (distance < size.x) return new Vector2(halfWidth - distance, -halfHeight);

            distance -= size.x;
            return new Vector2(-halfWidth, -halfHeight + distance);
        }

        private static float Angle(Random random) =>
            (float)random.NextDouble() * Mathf.PI * 2f;

        private static Vector2 Direction(float angle) =>
            new(Mathf.Cos(angle), Mathf.Sin(angle));

        private static float Range(Random random, float minimum, float maximum) =>
            Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
    }
}
