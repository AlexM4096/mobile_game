using System;
using Arch.Core;
using Arch.Unity.Conversion;
using UnityEngine;
using Object = UnityEngine.Object;
using EntityLifetime = _Project.Gameplay.Features.Lifetime.Lifetime;

namespace _Project.Gameplay.Features.LightningCharge
{
    public sealed class LightningArcFactory : IDisposable
    {
        private readonly World _world;
        private readonly LightningChargeConfig _settings;

        private Transform _viewsRoot;
        private Material _material;
        private int _viewIndex;

        public LightningArcFactory(World world, LightningChargeConfig settings)
        {
            _world = world;
            _settings = settings;
        }

        public Entity Create(Vector2 start, Vector2 end)
        {
            var view = new GameObject($"Lightning Arc {++_viewIndex}");
            view.transform.SetParent(GetViewsRoot(), false);

            var line = view.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(start.x, start.y, 0f));
            line.SetPosition(1, new Vector3(end.x, end.y, 0f));
            line.startWidth = _settings.ArcWidth;
            line.endWidth = _settings.ArcWidth;
            line.startColor = _settings.ArcColor;
            line.endColor = _settings.ArcColor;
            line.numCapVertices = 2;
            line.sortingOrder = 2;

            var material = GetMaterial();
            if (material != null)
            {
                line.sharedMaterial = material;
            }

            return _world.Create(
                new EntityLifetime { Value = _settings.ArcLifetime },
                new GameObjectReference(view));
        }

        public void Dispose()
        {
            DestroyObject(_material);
            if (_viewsRoot != null)
            {
                DestroyObject(_viewsRoot.gameObject);
            }
        }

        private Transform GetViewsRoot()
        {
            if (_viewsRoot == null)
            {
                _viewsRoot = new GameObject("Spawned Lightning Arc Views").transform;
            }

            return _viewsRoot;
        }

        private Material GetMaterial()
        {
            if (_material != null)
            {
                return _material;
            }

            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ??
                         Shader.Find("Sprites/Default");
            if (shader != null)
            {
                _material = new Material(shader)
                {
                    name = "Runtime Lightning Arc Material"
                };
            }

            return _material;
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
