using System;
using UnityEngine;
using Random = System.Random;

namespace _Project.Gameplay.Infrastructure
{
    internal static class SwarmSpawnGenerator
    {
        public static Vector2 GeneratePosition(
            Random random,
            SpawnShape shape,
            bool spawnOnEdge,
            float circleRadius,
            float boxWidth,
            float boxHeight)
        {
            return (shape, spawnOnEdge) switch
            {
                (SpawnShape.Circle, false) => GenerateInsideCircle(random, circleRadius),
                (SpawnShape.Circle, true) => GenerateOnCircleEdge(random, circleRadius),
                (SpawnShape.Box, false) => GenerateInsideBox(random, boxWidth, boxHeight),
                (SpawnShape.Box, true) => GenerateOnBoxEdge(random, boxWidth, boxHeight),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static float ApplyNoise(Random random, float baseValue, float noisePercentage)
        {
            var clampedNoise = Mathf.Clamp01(noisePercentage);
            var multiplier = Mathf.Lerp(
                1f - clampedNoise,
                1f + clampedNoise,
                (float)random.NextDouble());
            return baseValue * multiplier;
        }

        private static Vector2 GenerateInsideCircle(Random random, float radius)
        {
            var angle = RandomAngle(random);
            var distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
            return DirectionFromAngle(angle) * distance;
        }

        private static Vector2 GenerateOnCircleEdge(Random random, float radius)
        {
            return DirectionFromAngle(RandomAngle(random)) * radius;
        }

        private static Vector2 GenerateInsideBox(Random random, float width, float height)
        {
            return new Vector2(
                RandomRange(random, -width * 0.5f, width * 0.5f),
                RandomRange(random, -height * 0.5f, height * 0.5f));
        }

        private static Vector2 GenerateOnBoxEdge(Random random, float width, float height)
        {
            var halfWidth = width * 0.5f;
            var halfHeight = height * 0.5f;
            var distance = (float)random.NextDouble() * (2f * (width + height));

            if (distance < width)
            {
                return new Vector2(-halfWidth + distance, halfHeight);
            }

            distance -= width;
            if (distance < height)
            {
                return new Vector2(halfWidth, halfHeight - distance);
            }

            distance -= height;
            if (distance < width)
            {
                return new Vector2(halfWidth - distance, -halfHeight);
            }

            distance -= width;
            return new Vector2(-halfWidth, -halfHeight + distance);
        }

        private static float RandomAngle(Random random)
        {
            return (float)random.NextDouble() * Mathf.PI * 2f;
        }

        private static Vector2 DirectionFromAngle(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static float RandomRange(Random random, float minimum, float maximum)
        {
            return Mathf.Lerp(minimum, maximum, (float)random.NextDouble());
        }
    }
}
