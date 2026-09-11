using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace _Project.Gameplay.Features.EnemySpawn
{
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Configs/Gameplay/Enemy Spawn Config")]
    public sealed class EnemySpawnConfig : SerializedScriptableObject
    {
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private List<EnemyWaveConfig> waves = new();

        public int RandomSeed => randomSeed;
        public IReadOnlyList<EnemyWaveConfig> Waves => waves;
    }

    public enum SpawnShape
    {
        Circle,
        Box
    }

    [Serializable]
    public sealed class SpawnZone
    {
        [SerializeField] private SpawnShape shape = SpawnShape.Circle;
        [SerializeField] private Vector2 center;
        [SerializeField, ToggleLeft] private bool spawnOnEdge;
        [SerializeField, Min(0.01f)] private float circleRadius = 11f;
        [SerializeField, MinValue(0.01f)] private Vector2 boxSize = new(22f, 22f);

        public SpawnShape Shape => shape;
        public Vector2 Center => center;
        public bool SpawnOnEdge => spawnOnEdge;
        public float CircleRadius => circleRadius;
        public Vector2 BoxSize => boxSize;
    }

    [Serializable]
    public sealed class EnemyWaveEntry
    {
        [SerializeField, Required, InlineEditor] private EnemyConfig enemy;
        [SerializeField, Min(0)] private int count;
        [SerializeField, Min(0f)] private float spawnInterval = 1f;

        public EnemyConfig Enemy => enemy;
        public int Count => count;
        public float SpawnInterval => spawnInterval;
    }

    [Serializable]
    public sealed class EnemyWaveConfig
    {
        [SerializeField, Min(0f)] private float delay;
        [SerializeField, Min(0.01f)] private float duration = 30f;
        [SerializeField] private SpawnZone spawnZone = new();
        [SerializeField] private List<EnemyWaveEntry> composition = new();

        public float Delay => delay;
        public float Duration => duration;
        public SpawnZone SpawnZone => spawnZone;
        public IReadOnlyList<EnemyWaveEntry> Composition => composition;
    }
}
