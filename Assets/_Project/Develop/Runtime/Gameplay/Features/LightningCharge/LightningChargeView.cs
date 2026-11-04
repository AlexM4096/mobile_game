using UnityEngine;

namespace _Project.Gameplay.Features.LightningCharge
{
    public sealed class LightningChargeView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Color _originalColor;
        private bool _isInitialized;

        public void SetCharged(bool isCharged, Color chargedColor)
        {
            EnsureInitialized();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isCharged ? chargedColor : _originalColor;
            }
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
            }

            _isInitialized = true;
        }
    }
}
