namespace _Project.Gameplay.Features.EnemySpawn
{
    internal static class EnemySpawnConfigValidator
    {
        public static bool TryValidate(EnemySpawnConfig config, out string error)
        {
            if (config == null || config.Waves == null || config.Waves.Count == 0)
            {
                error = "Enemy spawn configuration requires at least one wave.";
                return false;
            }

            for (var waveIndex = 0; waveIndex < config.Waves.Count; waveIndex++)
            {
                var wave = config.Waves[waveIndex];
                if (!TryValidateWave(wave, waveIndex, out error))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateWave(
            EnemyWaveConfig wave,
            int waveIndex,
            out string error)
        {
            if (wave == null || wave.Delay < 0f || wave.Duration <= 0f)
            {
                error = $"Enemy spawn wave {waveIndex} is missing or has invalid timing.";
                return false;
            }

            if (!TryValidateZone(wave.SpawnZone, waveIndex, out error))
            {
                return false;
            }

            if (wave.Composition == null || wave.Composition.Count == 0)
            {
                error = $"Enemy spawn wave {waveIndex} requires composition entries.";
                return false;
            }

            for (var entryIndex = 0; entryIndex < wave.Composition.Count; entryIndex++)
            {
                if (!TryValidateEntry(wave.Composition[entryIndex], waveIndex, entryIndex, out error))
                {
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateZone(SpawnZone zone, int waveIndex, out string error)
        {
            if (zone == null ||
                zone.Shape == SpawnShape.Circle && zone.CircleRadius <= 0f ||
                zone.Shape == SpawnShape.Box && (zone.BoxSize.x <= 0f || zone.BoxSize.y <= 0f) ||
                zone.Shape != SpawnShape.Circle && zone.Shape != SpawnShape.Box)
            {
                error = $"Enemy spawn wave {waveIndex} has an invalid spawn zone.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateEntry(
            EnemyWaveEntry entry,
            int waveIndex,
            int entryIndex,
            out string error)
        {
            var enemy = entry?.Enemy;
            if (enemy == null || enemy.Prefab == null ||
                entry.Count < 0 || entry.Count > 1 && entry.SpawnInterval <= 0f ||
                enemy.BaseSpeed < 0f || enemy.BaseHealth < 1f || enemy.ColliderRadius <= 0f ||
                enemy.StatNoisePercentage < 0f || enemy.StatNoisePercentage > 1f)
            {
                error = $"Enemy spawn wave {waveIndex}, entry {entryIndex} is invalid.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
