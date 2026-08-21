#if UNITY_EDITOR
using UnityEngine;

namespace _Project.Gameplay.Features.PooledView
{
    public sealed partial class PooledViewCatalog
    {
        private void OnValidate()
        {
            if (!TryValidate(out var error))
            {
                Debug.LogError($"Invalid pooled-view catalog '{name}': {error}", this);
            }
        }
    }
}
#endif
