using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerMotionFeedbackView : MonoBehaviour
    {
        private const float FlashDuration = 0.12f;
        private static readonly Color FlashColor = new(1f, 0.9f, 0.48f, 1f);

        private readonly PlayerComboFireEffect _comboFire = new();
        private bool _comboFireActive;
        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;
        private TrailRenderer _trail;
        private Material _trailMaterial;
        private float _flashRemaining;

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.emitting = false;
            _trail.time = 0.16f;
            _trail.minVertexDistance = 0.08f;
            _trail.startWidth = 0.85f;
            _trail.endWidth = 0.05f;
            _trail.startColor = new Color(1f, 0.84f, 0.42f, 0.9f);
            _trail.endColor = new Color(1f, 0.5f, 0.18f, 0f);
            if (_spriteRenderer != null)
            {
                _trail.sortingLayerID = _spriteRenderer.sortingLayerID;
                _trail.sortingOrder = _spriteRenderer.sortingOrder - 1;
            }

            _trailMaterial = new Material(shader);
            _trail.sharedMaterial = _trailMaterial;
        }

        private void Update()
        {
            if (_flashRemaining <= 0f || _spriteRenderer == null) return;

            _flashRemaining = Mathf.Max(0f, _flashRemaining - Time.deltaTime);
            _spriteRenderer.color = Color.Lerp(_baseColor, FlashColor, _flashRemaining / FlashDuration);
        }

        private void OnDestroy()
        {
            _comboFire.Dispose();
            if (_trailMaterial != null)
            {
                Destroy(_trailMaterial);
            }
        }

        public void SetComboFire(bool active)
        {
            if (active == _comboFireActive) return;

            _comboFireActive = active;
            _comboFire.SetActive(active, transform, _spriteRenderer);
        }

        public void PlayLaunchBurst()
        {
            if (_spriteRenderer != null)
            {
                if (_flashRemaining <= 0f) _baseColor = _spriteRenderer.color;
                _flashRemaining = FlashDuration;
                _spriteRenderer.color = FlashColor;
            }

            if (_trail == null) return;
            _trail.Clear();
            _trail.emitting = true;
        }

        public void EndLaunchBurst()
        {
            if (_trail != null) _trail.emitting = false;
        }
    }
}
