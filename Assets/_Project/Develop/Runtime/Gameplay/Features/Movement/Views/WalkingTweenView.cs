using PrimeTween;
using UnityEngine;

namespace _Project.Gameplay.Features.Movement.Views
{
    [DisallowMultipleComponent]
    public sealed class WalkingTweenView : MonoBehaviour
    {
        private const float MinimumDuration = 0.01f;
        private const float DurationNoiseStrength = 0.15f;
        private const float MotionNoiseStrength = 0.2f;

        [SerializeField] private WalkingTweenConfig config;

        private Vector3 _initialScale;
        private Quaternion _initialRotation;
        private Tween _walkingTween;
        private bool _hasInitialPose;
        private Vector3 _firstScale;
        private Vector3 _secondScale;
        private Quaternion _firstRotation;
        private Quaternion _secondRotation;

        private void OnEnable()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(WalkingTweenView)} requires a {nameof(WalkingTweenConfig)}.", this);
                return;
            }

            CacheInitialPose();
            StartWalkingTween();
        }

        private void OnDisable()
        {
            StopWalkingTween();
            RestoreInitialPose();
        }

        private void CacheInitialPose()
        {
            _initialScale = transform.localScale;
            _initialRotation = transform.localRotation;
            _hasInitialPose = true;
        }

        private void StartWalkingTween()
        {
            var noise = config.Noise;
            var durationMultiplier = 1f
                + GetSignedNoise(1u) * DurationNoiseStrength * noise;
            var motionMultiplier = 1f
                + GetSignedNoise(2u) * MotionNoiseStrength * noise;
            var duration = Mathf.Max(
                MinimumDuration,
                config.StepDuration * durationMultiplier
            );
            var firstScaleMultiplier = Vector2.one
                + (config.FirstScaleMultiplier - Vector2.one) * motionMultiplier;
            var secondScaleMultiplier = Vector2.one
                + (config.SecondScaleMultiplier - Vector2.one) * motionMultiplier;
            var rotationAngle = config.RotationAngle * motionMultiplier;

            _firstScale = Vector3.Scale(
                _initialScale,
                new Vector3(firstScaleMultiplier.x, firstScaleMultiplier.y, 1f)
            );
            _secondScale = Vector3.Scale(
                _initialScale,
                new Vector3(secondScaleMultiplier.x, secondScaleMultiplier.y, 1f)
            );
            _firstRotation = _initialRotation * Quaternion.Euler(0f, 0f, rotationAngle);
            _secondRotation = _initialRotation * Quaternion.Euler(0f, 0f, -rotationAngle);

            _walkingTween = Tween.Custom(
                this,
                0f,
                1f,
                duration,
                static (view, progress) => view.ApplyPose(progress),
                config.Ease,
                cycles: -1,
                cycleMode: CycleMode.Yoyo
            );
            _walkingTween.progress = GetNoise(3u) * noise;
        }

        private void StopWalkingTween()
        {
            if (_walkingTween.isAlive)
            {
                _walkingTween.Stop();
            }
        }

        private void ApplyPose(float progress)
        {
            transform.localScale = Vector3.LerpUnclamped(_firstScale, _secondScale, progress);
            transform.localRotation = Quaternion.SlerpUnclamped(
                _firstRotation,
                _secondRotation,
                progress
            );
        }

        private void RestoreInitialPose()
        {
            if (!_hasInitialPose)
            {
                return;
            }

            transform.localScale = _initialScale;
            transform.localRotation = _initialRotation;
        }

        private float GetSignedNoise(uint salt)
        {
            return GetNoise(salt) * 2f - 1f;
        }

        private float GetNoise(uint salt)
        {
            var hash = unchecked((uint)GetInstanceID()) + salt * 0x9e3779b9u;
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            hash ^= hash >> 16;
            return (hash & 0x00ffffffu) / 16777216f;
        }
    }
}
