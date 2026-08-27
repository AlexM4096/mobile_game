using Arch.Core;
using Arch.Unity.Toolkit;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.Player.Systems
{
    public sealed class UpdatePlayerOrbitVisualizationSystem : UnitySystemBase
    {
        private const int TrajectorySegments = 96;
        private const int MarkerSegments = 32;
        private static readonly QueryDescription _description = new QueryDescription()
            .WithAll<PlayerTag, PlayerMotion>();
        private readonly PlayerOrbitVisualizationSettings _settings;
        private GameObject _root;
        private LineRenderer _trajectory;
        private LineRenderer _marker;
        private Material _material;

        public UpdatePlayerOrbitVisualizationSystem(
            World world, PlayerOrbitVisualizationSettings settings) : base(world)
        {
            _settings = settings;
        }

        public override void Initialize()
        {
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("Player orbit visualization requires the Sprites/Default shader.");
                return;
            }

            _material = new Material(shader)
            {
                name = "Runtime Player Orbit Material",
                color = _settings.Color
            };
            _root = new GameObject("Player Orbit Visualization");
            _trajectory = CreateLineRenderer("Trajectory", TrajectorySegments);
            _marker = CreateLineRenderer("Orbit Point", MarkerSegments);
            SetVisible(false);
        }

        public override void Update(in SystemState state)
        {
            if (_root == null) return;

            var hasOrbit = false;
            var center = Vector2.zero;
            var radius = 0f;
            World.Query(in _description, (ref PlayerMotion motion) =>
            {
                if (hasOrbit || motion.Mode != PlayerMotionMode.Orbit) return;
                hasOrbit = true;
                center = motion.OrbitCenter;
                radius = motion.OrbitRadius;
            });

            if (!hasOrbit)
            {
                SetVisible(false);
                return;
            }

            UpdateCircle(_trajectory, center, radius, TrajectorySegments);
            UpdateCircle(_marker, center, _settings.MarkerRadius, MarkerSegments);
            SetVisible(true);
        }

        public override void Dispose()
        {
            if (_root != null) Object.Destroy(_root);
            if (_material != null) Object.Destroy(_material);
            base.Dispose();
        }

        private LineRenderer CreateLineRenderer(string objectName, int segments)
        {
            var lineObject = new GameObject(objectName);
            lineObject.transform.SetParent(_root.transform, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = segments;
            line.startWidth = _settings.LineWidth;
            line.endWidth = _settings.LineWidth;
            line.startColor = _settings.Color;
            line.endColor = _settings.Color;
            line.sharedMaterial = _material;
            line.numCornerVertices = 2;
            return line;
        }

        private static void UpdateCircle(LineRenderer line, Vector2 center, float radius, int segments)
        {
            for (var index = 0; index < segments; index++)
            {
                var angle = index * Mathf.PI * 2f / segments;
                line.SetPosition(index, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius,
                    0f));
            }
        }

        private void SetVisible(bool visible)
        {
            _trajectory.enabled = visible;
            _marker.enabled = visible;
        }
    }
}
