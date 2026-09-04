#if UNITY_EDITOR
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Common;
using UnityEditor;
using UnityEngine;

namespace _Project.Gameplay.Features.Collision.Systems
{
    public sealed class ColliderDebugSystem : UnitySystemBase
    {
        private const int CircleSegments = 48;
        private const float GameViewDepth = -0.1f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Position, CollisionBody>();

        private readonly ColliderDebugConfig _config;
        private readonly List<ColliderDebugShape> _shapes = new();
        private readonly List<LineRenderer> _gameViewRenderers = new();
        private readonly Vector3[] _boxPoints = new Vector3[5];

        internal IReadOnlyList<ColliderDebugShape> Shapes => _shapes;

        private GameObject _gameViewRoot;
        private Material _gameViewMaterial;

        public ColliderDebugSystem(World world, ColliderDebugConfig config) : base(world)
        {
            _config = config;
        }

        public override void Initialize()
        {
            SceneView.duringSceneGui += DrawColliders;
            InitializeGameViewRendering();
        }

        public override void Update(in SystemState state)
        {
            _shapes.Clear();
            if (_config == null || !_config.Enabled)
            {
                UpdateGameViewRendering();
                SceneView.RepaintAll();
                return;
            }

            World.Query(in _description, (
                Entity entity,
                ref Position position,
                ref CollisionBody body
            ) =>
            {
                if (body.Layer == CollisionLayer.None ||
                    !CollisionShapeFactory.TryCreate(World, entity, position.Value, out var shape))
                {
                    return;
                }

                _shapes.Add(new ColliderDebugShape(body.Layer, shape));
            });

            UpdateGameViewRendering();
            SceneView.RepaintAll();
        }

        public override void Dispose()
        {
            SceneView.duringSceneGui -= DrawColliders;
            _shapes.Clear();
            DestroyEditorObject(_gameViewRoot);
            DestroyEditorObject(_gameViewMaterial);
            base.Dispose();
        }

        private void InitializeGameViewRendering()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("Collider debug visualization requires the Sprites/Default shader.");
                return;
            }

            _gameViewMaterial = new Material(shader)
            {
                name = "Runtime Collider Debug Material",
                color = Color.white,
                hideFlags = HideFlags.HideAndDontSave
            };
            _gameViewRoot = new GameObject("Collider Debug Visualization")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneVisibilityManager.instance.Hide(_gameViewRoot, true);
        }

        private void UpdateGameViewRendering()
        {
            var visibleCount = _config != null && _config.Enabled && _gameViewRoot != null
                ? _shapes.Count
                : 0;
            EnsureGameViewRendererCount(visibleCount);

            for (var index = 0; index < _gameViewRenderers.Count; index++)
            {
                var line = _gameViewRenderers[index];
                var visible = index < visibleCount;
                line.enabled = visible;
                if (!visible)
                {
                    continue;
                }

                UpdateGameViewRenderer(line, _shapes[index]);
            }

        }

        private void EnsureGameViewRendererCount(int requiredCount)
        {
            while (_gameViewRenderers.Count < requiredCount)
            {
                var lineObject = new GameObject(
                    $"Collider Outline {_gameViewRenderers.Count + 1}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lineObject.transform.SetParent(_gameViewRoot.transform, false);
                SceneVisibilityManager.instance.Hide(lineObject, false);

                var line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.loop = true;
                line.sharedMaterial = _gameViewMaterial;
                line.numCornerVertices = 2;
                line.numCapVertices = 0;
                line.sortingOrder = short.MaxValue;
                _gameViewRenderers.Add(line);
            }
        }

        private void UpdateGameViewRenderer(
            LineRenderer line,
            ColliderDebugShape debugShape)
        {
            var color = _config.GetColor(debugShape.Layer);
            line.startColor = color;
            line.endColor = color;
            line.startWidth = _config.GameViewLineWidth;
            line.endWidth = _config.GameViewLineWidth;

            var shape = debugShape.Shape;
            if (shape.Type == CollisionShapeType.Circle)
            {
                line.positionCount = CircleSegments;
                for (var index = 0; index < CircleSegments; index++)
                {
                    var angle = index * Mathf.PI * 2f / CircleSegments;
                    line.SetPosition(index, new Vector3(
                        shape.Position.x + Mathf.Cos(angle) * shape.Radius,
                        shape.Position.y + Mathf.Sin(angle) * shape.Radius,
                        GameViewDepth));
                }

                return;
            }

            line.positionCount = 4;
            SetGameViewBoxPoint(line, 0, shape, -shape.HalfExtents.x, -shape.HalfExtents.y);
            SetGameViewBoxPoint(line, 1, shape, shape.HalfExtents.x, -shape.HalfExtents.y);
            SetGameViewBoxPoint(line, 2, shape, shape.HalfExtents.x, shape.HalfExtents.y);
            SetGameViewBoxPoint(line, 3, shape, -shape.HalfExtents.x, shape.HalfExtents.y);
        }

        private static void SetGameViewBoxPoint(
            LineRenderer line,
            int index,
            CollisionShape shape,
            float horizontal,
            float vertical)
        {
            var point = shape.Position +
                        shape.AxisX * horizontal +
                        shape.AxisY * vertical;
            line.SetPosition(index, new Vector3(point.x, point.y, GameViewDepth));
        }

        private void DrawColliders(SceneView sceneView)
        {
            if (_config == null || !_config.Enabled)
            {
                return;
            }

            foreach (var debugShape in _shapes)
            {
                Handles.color = _config.GetColor(debugShape.Layer);
                var shape = debugShape.Shape;
                if (shape.Type == CollisionShapeType.Circle)
                {
                    Handles.DrawWireDisc(
                        new Vector3(shape.Position.x, shape.Position.y, 0f),
                        Vector3.forward,
                        shape.Radius,
                        _config.LineWidth);
                    continue;
                }

                SetBoxPoint(0, shape, -shape.HalfExtents.x, -shape.HalfExtents.y);
                SetBoxPoint(1, shape, shape.HalfExtents.x, -shape.HalfExtents.y);
                SetBoxPoint(2, shape, shape.HalfExtents.x, shape.HalfExtents.y);
                SetBoxPoint(3, shape, -shape.HalfExtents.x, shape.HalfExtents.y);
                _boxPoints[4] = _boxPoints[0];
                Handles.DrawAAPolyLine(_config.LineWidth, _boxPoints);
            }
        }

        private void SetBoxPoint(
            int index,
            CollisionShape shape,
            float horizontal,
            float vertical)
        {
            var point = shape.Position +
                        shape.AxisX * horizontal +
                        shape.AxisY * vertical;
            _boxPoints[index] = new Vector3(point.x, point.y, 0f);
        }

        private static void DestroyEditorObject(Object target)
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

    internal readonly struct ColliderDebugShape
    {
        public ColliderDebugShape(CollisionLayer layer, CollisionShape shape)
        {
            Layer = layer;
            Shape = shape;
        }

        public CollisionLayer Layer { get; }
        public CollisionShape Shape { get; }
    }
}
#endif
