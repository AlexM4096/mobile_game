using UnityEngine;

namespace _Project.Gameplay.Features.Spear
{
    public sealed class SpearView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public void SetSize(Vector2 worldSize)
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
            {
                return;
            }

            spriteRenderer.transform.localScale = new Vector3(
                worldSize.y / spriteSize.x,
                worldSize.x / spriteSize.y,
                1f);
        }
    }
}
