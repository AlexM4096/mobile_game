using UnityEngine;

namespace _Project.Gameplay.Features.PooledView
{
    public sealed class CameraViewVisibilityService : IViewVisibilityService
    {
        private readonly float _viewportMargin;
        private Camera _camera;
        private bool _missingCameraLogged;

        public CameraViewVisibilityService(PooledViewCatalog catalog)
        {
            _viewportMargin = catalog.ViewportMargin;
        }

        public bool IsVisible(Vector2 position)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                if (!_missingCameraLogged)
                {
                    Debug.LogWarning("Pooled-view visibility has no gameplay camera; views will remain visible.");
                    _missingCameraLogged = true;
                }

                return true;
            }

            _missingCameraLogged = false;
            var viewportPosition = _camera.WorldToViewportPoint(
                new Vector3(position.x, position.y, 0f));

            return viewportPosition.z > 0f &&
                   viewportPosition.x >= -_viewportMargin &&
                   viewportPosition.x <= 1f + _viewportMargin &&
                   viewportPosition.y >= -_viewportMargin &&
                   viewportPosition.y <= 1f + _viewportMargin;
        }
    }
}
