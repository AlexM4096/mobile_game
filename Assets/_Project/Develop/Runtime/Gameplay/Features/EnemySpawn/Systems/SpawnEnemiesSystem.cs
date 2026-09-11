using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using _Project.Gameplay.Features.Player;
using UnityEngine;

namespace _Project.Gameplay.Features.EnemySpawn.Systems
{
    public sealed class SpawnEnemiesSystem : UnitySystemBase
    {
        private static readonly QueryDescription PlayerDescription =
            new QueryDescription().WithAll<PlayerTag, Position>();

        private readonly EnemySpawnConfig _config;
        private readonly EnemyFactory _factory;
        private readonly List<ActiveEmission> _activeEmissions = new();
        private System.Random _random;
        private bool _isEnabled;
        private bool _timelineStarted;
        private int _nextWaveIndex;
        private double _nextWaveStartTime;

        public SpawnEnemiesSystem(
            World world,
            EnemySpawnConfig config,
            EnemyFactory factory) : base(world)
        {
            _config = config;
            _factory = factory;
        }

        public override void Initialize()
        {
            _isEnabled = EnemySpawnConfigValidator.TryValidate(_config, out var error);
            if (!_isEnabled)
            {
                Debug.LogError(error);
                return;
            }

            _random = new System.Random(_config.RandomSeed);
        }

        public override void Update(in SystemState state)
        {
            if (!_isEnabled) return;

            if (!_timelineStarted)
            {
                _timelineStarted = true;
                _nextWaveStartTime = state.Time + _config.Waves[0].Delay;
            }

            while (TryGetNextDueEvent(state.Time, out var emissionIndex, out var activateWave))
            {
                if (activateWave)
                {
                    ActivateNextWave();
                    continue;
                }

                if (!TryGetPlayer(out var target))
                {
                    return;
                }

                SpawnFrom(emissionIndex, target);
            }
        }

        private void ActivateNextWave()
        {
            var waveIndex = _nextWaveIndex;
            var wave = _config.Waves[waveIndex];
            var startTime = _nextWaveStartTime;
            for (var entryIndex = 0; entryIndex < wave.Composition.Count; entryIndex++)
            {
                var entry = wave.Composition[entryIndex];
                if (entry.Count == 0) continue;

                _activeEmissions.Add(new ActiveEmission(
                    waveIndex,
                    entryIndex,
                    entry,
                    wave.SpawnZone,
                    startTime));
            }

            _nextWaveIndex++;
            if (_nextWaveIndex < _config.Waves.Count)
            {
                _nextWaveStartTime =
                    startTime + wave.Duration + _config.Waves[_nextWaveIndex].Delay;
            }
        }

        private bool TryGetNextDueEvent(
            double currentTime,
            out int emissionIndex,
            out bool activateWave)
        {
            emissionIndex = FindEarliestEmission();
            var hasWave = _nextWaveIndex < _config.Waves.Count;
            var emissionTime = emissionIndex >= 0
                ? _activeEmissions[emissionIndex].NextSpawnTime
                : double.PositiveInfinity;

            activateWave = hasWave && _nextWaveStartTime < emissionTime;
            if (!activateWave && emissionIndex < 0 && hasWave)
            {
                activateWave = true;
            }

            var eventTime = activateWave ? _nextWaveStartTime : emissionTime;
            return eventTime <= currentTime;
        }

        private int FindEarliestEmission()
        {
            var result = -1;
            for (var index = 0; index < _activeEmissions.Count; index++)
            {
                if (result < 0 || IsEarlier(_activeEmissions[index], _activeEmissions[result]))
                {
                    result = index;
                }
            }

            return result;
        }

        private static bool IsEarlier(ActiveEmission candidate, ActiveEmission current)
        {
            if (candidate.NextSpawnTime != current.NextSpawnTime)
            {
                return candidate.NextSpawnTime < current.NextSpawnTime;
            }

            if (candidate.WaveIndex != current.WaveIndex)
            {
                return candidate.WaveIndex < current.WaveIndex;
            }

            return candidate.EntryIndex < current.EntryIndex;
        }

        private void SpawnFrom(int emissionIndex, Entity target)
        {
            var emission = _activeEmissions[emissionIndex];
            var position = SpawnPositionGenerator.GeneratePosition(_random, emission.Zone);
            _factory.Create(emission.Entry.Enemy, position, target, _random);
            emission.Remaining--;
            if (emission.Remaining == 0)
            {
                _activeEmissions.RemoveAt(emissionIndex);
                return;
            }

            emission.NextSpawnTime += emission.Entry.SpawnInterval;
        }

        private bool TryGetPlayer(out Entity player)
        {
            var result = default(Entity);
            var found = false;
            World.Query(in PlayerDescription, (Entity entity, ref Position _) =>
            {
                if (found) return;
                result = entity;
                found = true;
            });
            player = result;
            return found;
        }

        private sealed class ActiveEmission
        {
            public readonly int WaveIndex;
            public readonly int EntryIndex;
            public readonly EnemyWaveEntry Entry;
            public readonly SpawnZone Zone;
            public int Remaining;
            public double NextSpawnTime;

            public ActiveEmission(
                int waveIndex,
                int entryIndex,
                EnemyWaveEntry entry,
                SpawnZone zone,
                double nextSpawnTime)
            {
                WaveIndex = waveIndex;
                EntryIndex = entryIndex;
                Entry = entry;
                Zone = zone;
                Remaining = entry.Count;
                NextSpawnTime = nextSpawnTime;
            }
        }
    }
}
