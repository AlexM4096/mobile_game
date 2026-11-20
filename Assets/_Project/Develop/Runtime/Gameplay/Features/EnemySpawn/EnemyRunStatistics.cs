using System;

namespace _Project.Gameplay.Features.EnemySpawn
{
    public sealed class EnemyRunStatistics
    {
        public const int KillsPerComboTier = 10;
        private const float InitialComboWindow = 3.2f;
        private const float ComboWindowReduction = 0.3f;
        private const float MinimumComboWindow = 0.9f;
        private const float InitialComboCharge = 0.65f;
        private const float ChargePerKill = 0.15f;

        private int _comboCount;
        private double _comboDeadline;
        private float _comboWindow;

        public int Killed { get; private set; }
        public int Escaped { get; private set; }
        public int ComboMilestones { get; private set; }

        public void RecordKill(double time)
        {
            Killed++;
            var current = GetCombo(time);
            var charge = current.Count == 0
                ? InitialComboCharge
                : Math.Min(1f, current.Remaining + ChargePerKill);
            _comboCount = current.Count + 1;
            var tier = _comboCount / KillsPerComboTier;
            _comboWindow = Math.Max(MinimumComboWindow, InitialComboWindow - tier * ComboWindowReduction);
            _comboDeadline = time + _comboWindow * charge;
            if (_comboCount % KillsPerComboTier == 0)
            {
                ComboMilestones++;
            }
        }

        public KillComboSnapshot GetCombo(double time)
        {
            return _comboCount == 0 || time >= _comboDeadline
                ? default
                : new KillComboSnapshot(_comboCount,
                    (float)Math.Min(1d, (_comboDeadline - time) / _comboWindow));
        }

        public void RecordEscapes(int count) => Escaped += count;
    }

    public readonly struct KillComboSnapshot
    {
        public readonly int Count;
        public readonly float Remaining;

        public KillComboSnapshot(int count, float remaining)
        {
            Count = count;
            Remaining = remaining;
        }
    }
}
