using System;
using DG.Tweening;
using _Project.Gameplay.Features.Movement.Views;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.Death
{
    public sealed class EnemyDeathAnimation : IDisposable
    {
        private const int MaximumVisibleDeaths = 48;
        private const float Duration = 0.42f;
        private const float FadeDelay = 0.07f;

        private readonly EnemyDefeatParticles _particles = new();
        private Transform _root;

        public bool TryPlay(GameObject view, Vector2 position, Vector2 hitDirection, bool comboMilestone)
        {
            if (!Application.isPlaying || view == null ||
                (_root != null && _root.childCount >= MaximumVisibleDeaths))
            {
                return false;
            }

            var renderers = view.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0) return false;

            if (_root == null)
            {
                _root = new GameObject("Enemy Death Views").transform;
            }

            var walking = view.GetComponentInChildren<WalkingTweenView>();
            if (walking != null && walking.enabled)
            {
                var visual = walking.transform;
                var scale = visual.localScale;
                var rotation = visual.localRotation;
                walking.enabled = false;
                visual.localScale = scale;
                visual.localRotation = rotation;
            }

            var target = view.transform;
            target.SetParent(_root, true);
            var origin = new Vector3(position.x, position.y, target.position.z);
            target.position = origin;
            var side = (view.GetInstanceID() & 4) == 0 ? -1f : 1f;
            var direction = hitDirection.sqrMagnitude > 0.0001f
                ? hitDirection.normalized
                : new Vector2(side, 0.55f).normalized;
            _particles.Emit(origin, renderers[0], direction, comboMilestone);
            var destination = origin + (Vector3)(direction * 0.64f + Vector2.up * 0.18f);
            var tilt = Mathf.Abs(direction.x) > 0.1f ? Mathf.Sign(direction.x) : side;
            var endRotation = target.rotation * Quaternion.Euler(0f, 0f, -tilt * 15f);
            var initialAlphas = new float[renderers.Length];
            for (var index = 0; index < renderers.Length; index++)
            {
                initialAlphas[index] = renderers[index].color.a;
            }

            DOTween.Sequence()
                .SetLink(view)
                .SetRecyclable(true)
                .Append(target.DOMove(destination, Duration).SetEase(Ease.OutCubic))
                .Join(target.DORotateQuaternion(endRotation, Duration).SetEase(Ease.OutCubic))
                .Join(target.DOScale(target.localScale * 0.82f, Duration).SetEase(Ease.InCubic))
                .Insert(FadeDelay, DOVirtual.Float(1f, 0f, Duration - FadeDelay,
                    opacity => Fade(renderers, initialAlphas, opacity)))
                .OnComplete(() => Object.Destroy(view));

            return true;
        }

        public void Dispose()
        {
            _particles.Dispose();
            if (_root != null) Object.Destroy(_root.gameObject);
        }

        private static void Fade(SpriteRenderer[] renderers, float[] initialAlphas, float opacity)
        {
            for (var index = 0; index < renderers.Length; index++)
            {
                var renderer = renderers[index];
                if (renderer == null) continue;

                var color = renderer.color;
                color.a = initialAlphas[index] * opacity;
                renderer.color = color;
            }
        }
    }
}
