using System;
using Arch.Core;
using Arch.Unity.Conversion;
using _Project.Gameplay.Features.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.RainOfArrows
{
    public sealed class RainOfArrowsFactory : IDisposable
    {
        private const int CircleSegments = 96;
        private const int DashTextureWidth = 16;

        private readonly World _world;
        private readonly RainOfArrowsConfig _settings;
        private readonly Grid _environmentGrid;
        private readonly System.Random _random = new();
        private Transform _viewsRoot;
        private Material _outlineMaterial;
        private Texture2D _dashTexture;
        private int _zoneViewIndex;
        private int _projectileViewIndex;

        public RainOfArrowsFactory(
            World world,
            RainOfArrowsConfig settings,
            Grid environmentGrid)
        {
            _world = world;
            _settings = settings;
            _environmentGrid = environmentGrid;
        }

        public Entity CreateZone(
            Entity owner,
            Vector2 center,
            in RainOfArrowsAbility ability,
            double currentTime)
        {
            var fallHeight = _settings.GetFallHeight(_environmentGrid);
            var fallTime = fallHeight / ability.ProjectileSpeed;
            var launchWindow = Mathf.Max(0f, ability.Duration - fallTime);
            var launchInterval = ability.ProjectileCount > 1
                ? launchWindow / (ability.ProjectileCount - 1)
                : 0f;
            var zoneView = CreateDashedCircle(
                $"Rain Of Arrows Zone {++_zoneViewIndex}",
                center,
                ability.ZoneRadius);

            return _world.Create(
                new RainOfArrowsZone
                {
                    Owner = owner,
                    EndTime = currentTime + ability.Duration,
                    NextLaunchTime = currentTime,
                    LaunchInterval = launchInterval,
                    ZoneRadius = ability.ZoneRadius,
                    ImpactRadius = ability.ImpactRadius,
                    ProjectileSpeed = ability.ProjectileSpeed,
                    FallHeight = fallHeight,
                    ProjectileCount = ability.ProjectileCount,
                    LaunchedProjectiles = 0,
                    DamageHits = ability.DamageHits
                },
                new Position { Value = center },
                new GameObjectReference(zoneView));
        }

        public Entity CreateProjectile(
            Vector2 impactPoint,
            float fallHeight,
            float speed,
            float impactRadius,
            int damageHits,
            double impactTime,
            double currentTime)
        {
            var remainingDistance = Mathf.Clamp(
                (float)((impactTime - currentTime) * speed),
                0f,
                fallHeight);
            var origin = impactPoint + Vector2.up * remainingDistance;
            var projectile = _world.Create(
                new RainOfArrowsProjectile
                {
                    ImpactPoint = impactPoint,
                    ImpactRadius = impactRadius,
                    Speed = speed,
                    DamageHits = damageHits,
                    ImpactTime = impactTime
                },
                new Position { Value = origin });

            var view = Object.Instantiate(_settings.ProjectilePrefab, GetViewsRoot());
            view.name = $"Rain Arrow View {++_projectileViewIndex}";
            view.transform.position = new Vector3(origin.x, origin.y, 0f);
            if (view.TryGetComponent<RainOfArrowsView>(out var arrowView))
            {
                var spriteIndex = _random.Next(_settings.ProjectileSprites.Count);
                arrowView.SetSprite(_settings.ProjectileSprites[spriteIndex]);
            }

            _world.Add(projectile, new GameObjectReference(view));
            return projectile;
        }

        public LineRenderer CreateTargetingRangeRenderer()
        {
            var rangeObject = CreateDashedCircle(
                "Rain Of Arrows Targeting Range",
                Vector2.zero,
                _settings.GetTargetingRange(_environmentGrid));
            var line = rangeObject.GetComponent<LineRenderer>();
            line.enabled = false;
            return line;
        }

        public void UpdateCircle(LineRenderer line, Vector2 center, float radius)
        {
            if (line == null)
            {
                return;
            }

            line.positionCount = CircleSegments;
            for (var index = 0; index < CircleSegments; index++)
            {
                var angle = index * Mathf.PI * 2f / CircleSegments;
                line.SetPosition(index, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    0f));
            }
        }

        public void Dispose()
        {
            DestroyObject(_viewsRoot == null ? null : _viewsRoot.gameObject);
            DestroyObject(_outlineMaterial);
            DestroyObject(_dashTexture);
            _viewsRoot = null;
            _outlineMaterial = null;
            _dashTexture = null;
        }

        private GameObject CreateDashedCircle(string objectName, Vector2 center, float radius)
        {
            var circle = new GameObject(objectName);
            circle.transform.SetParent(GetViewsRoot(), false);
            var line = circle.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.startWidth = _settings.OutlineWidth;
            line.endWidth = _settings.OutlineWidth;
            line.startColor = _settings.OutlineColor;
            line.endColor = _settings.OutlineColor;
            line.sharedMaterial = GetOutlineMaterial();
            line.textureMode = LineTextureMode.Tile;
            line.numCornerVertices = 2;
            line.sortingOrder = 2;
            UpdateCircle(line, center, radius);
            return circle;
        }

        private Material GetOutlineMaterial()
        {
            if (_outlineMaterial != null)
            {
                return _outlineMaterial;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Rain of Arrows visualization requires the Sprites/Default shader.");
            }

            _dashTexture = new Texture2D(
                DashTextureWidth,
                1,
                TextureFormat.RGBA32,
                false)
            {
                name = "Runtime Rain Of Arrows Dash Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            for (var index = 0; index < DashTextureWidth; index++)
            {
                _dashTexture.SetPixel(
                    index,
                    0,
                    index < DashTextureWidth / 2 ? Color.white : Color.clear);
            }

            _dashTexture.Apply(false, true);
            _outlineMaterial = new Material(shader)
            {
                name = "Runtime Rain Of Arrows Outline Material",
                color = _settings.OutlineColor,
                mainTexture = _dashTexture
            };
            _outlineMaterial.mainTextureScale = new Vector2(
                1f / _settings.DashLength,
                1f);
            return _outlineMaterial;
        }

        private Transform GetViewsRoot()
        {
            if (_viewsRoot == null)
            {
                _viewsRoot = new GameObject("Spawned Rain Of Arrows Views").transform;
            }

            return _viewsRoot;
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
