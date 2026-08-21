using UnityEngine;

namespace _Project.Gameplay.Features.PooledView
{
    public interface IViewPool
    {
        bool TryGetReleaseDelay(int poolId, out float releaseDelay);

        GameObject Get(int poolId);
        bool TryGet(int poolId, out GameObject view);

        bool Release(GameObject view);
        bool ActivateIfPending(GameObject view);
    }
}
