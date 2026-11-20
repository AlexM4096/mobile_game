using System;
using Arch.Core;
using _Project.Gameplay.Features.Collision;
using _Project.Gameplay.Features.Common;
using UnityEngine;
using Object = UnityEngine.Object;
using BoxColliderComponent = _Project.Gameplay.Features.Collision.BoxCollider;

namespace _Project.Gameplay.Features.Arena
{
    /// <summary>
    /// Creates the four solid walls that enclose the playable arena.
    /// The walls use the project's ECS collision system, not Unity Physics 2D.
    /// </summary>
    public sealed class ArenaBoundaryFactory : IDisposable
    {
        private static readonly Color WallColor = new(0.08f, 0.16f, 0.28f, 0.95f);

        private readonly World _world;
        private readonly ArenaBoundsConfig _configuration;
        private Transform _viewsRoot;
        private Sprite _wallSprite;

        public ArenaBoundaryFactory(World world, ArenaBoundsConfig configuration = null)
        {
            _world = world;
            _configuration = configuration;
        }

        public void Create()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("Arena bounds configuration is missing.");
            }

            if (!_configuration.TryValidate(out var error))
            {
                throw new InvalidOperationException($"Arena bounds configuration is invalid. {error}");
            }

            var center = _configuration.Center;
            var halfWidth = _configuration.HorizontalHalfSize;
            var halfHeight = _configuration.VerticalHalfSize;
            Create(new Rect(center.x - halfWidth, center.y - halfHeight, 2f * halfWidth, 2f * halfHeight),
                _configuration.WallThickness);
        }

        public void Create(Rect bounds, float thickness)
        {
            var center = bounds.center;
            var halfWidth = bounds.width * 0.5f;
            var halfHeight = bounds.height * 0.5f;
            var halfThickness = thickness * 0.5f;

            CreateWall(
                "Arena Wall Top",
                center + new Vector2(0f, halfHeight + halfThickness),
                new Vector2(halfWidth + thickness, halfThickness));
            CreateWall(
                "Arena Wall Bottom",
                center + new Vector2(0f, -halfHeight - halfThickness),
                new Vector2(halfWidth + thickness, halfThickness));
            CreateWall(
                "Arena Wall Left",
                center + new Vector2(-halfWidth - halfThickness, 0f),
                new Vector2(halfThickness, halfHeight));
            CreateWall(
                "Arena Wall Right",
                center + new Vector2(halfWidth + halfThickness, 0f),
                new Vector2(halfThickness, halfHeight));
        }

        public void Dispose()
        {
            if (_viewsRoot != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_viewsRoot.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(_viewsRoot.gameObject);
                }
            }

            if (_wallSprite != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_wallSprite);
                }
                else
                {
                    Object.DestroyImmediate(_wallSprite);
                }
            }
        }

        private void CreateWall(string wallName, Vector2 position, Vector2 halfExtents)
        {
            _world.Create(
                new Position { Value = position },
                new CollisionBody
                {
                    BodyType = ColliderBodyType.Static,
                    Layer = CollisionLayer.Environment
                },
                new BoxColliderComponent { HalfExtents = halfExtents });

            var view = new GameObject(wallName);
            view.transform.SetParent(GetViewsRoot(), false);
            view.transform.position = position;

            var renderer = view.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWallSprite();
            view.transform.localScale = new Vector3(halfExtents.x * 2f, halfExtents.y * 2f, 1f);
            renderer.color = WallColor;
            renderer.sortingOrder = 5;
        }

        private Transform GetViewsRoot()
        {
            if (_viewsRoot == null)
            {
                _viewsRoot = new GameObject("Arena Boundaries").transform;
            }

            return _viewsRoot;
        }

        private Sprite GetWallSprite()
        {
            if (_wallSprite == null)
            {
                _wallSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f,
                    0,
                    SpriteMeshType.FullRect);
            }

            return _wallSprite;
        }
    }
}
