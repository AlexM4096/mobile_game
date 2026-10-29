using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Utilities
{
    public interface IGameplayPauseService
    {
        bool IsPaused { get; }
        IDisposable Acquire(string reason);
    }

    public sealed class GameplayPauseService : IGameplayPauseService, IDisposable
    {
        private readonly HashSet<PauseLease> _leases = new();
        private float _timeScaleBeforePause = 1f;
        private bool _isDisposed;

        public bool IsPaused => _leases.Count > 0;

        public IDisposable Acquire(string reason)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(GameplayPauseService));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("A pause reason is required.", nameof(reason));
            }

            if (_leases.Count == 0)
            {
                _timeScaleBeforePause = Time.timeScale;
                Time.timeScale = 0f;
            }

            var lease = new PauseLease(this, reason);
            _leases.Add(lease);
            return lease;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            foreach (var lease in _leases)
            {
                lease.Detach();
            }

            _leases.Clear();
            RestoreTimeScale();
        }

        private void Release(PauseLease lease)
        {
            if (!_leases.Remove(lease) || _leases.Count > 0)
            {
                return;
            }

            RestoreTimeScale();
        }

        private void RestoreTimeScale()
        {
            Time.timeScale = _timeScaleBeforePause;
        }

        private sealed class PauseLease : IDisposable
        {
            private GameplayPauseService _owner;

            public PauseLease(GameplayPauseService owner, string reason)
            {
                _owner = owner;
                Reason = reason;
            }

            public string Reason { get; }

            public void Dispose()
            {
                var owner = _owner;
                _owner = null;
                owner?.Release(this);
            }

            public void Detach()
            {
                _owner = null;
            }
        }
    }
}
