using UnityEngine;

namespace _Project.Gameplay.Features.Movement.Views
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyGameObjectView : MonoBehaviour
    {
        private static Sprite sharedSprite;

        [SerializeField] private Color color = new(0.3f, 0.85f, 1f, 1f);

        private void Awake()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite ??= GetSharedSprite();
            spriteRenderer.color = color;
        }

        private static Sprite GetSharedSprite()
        {
            if (sharedSprite != null)
            {
                return sharedSprite;
            }

            sharedSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return sharedSprite;
        }
    }
}
