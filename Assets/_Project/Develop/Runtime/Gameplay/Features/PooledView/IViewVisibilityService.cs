using UnityEngine;

namespace _Project.Gameplay.Features.PooledView
{
    public interface IViewVisibilityService
    {
        bool IsVisible(Vector2 position);
    }
}
