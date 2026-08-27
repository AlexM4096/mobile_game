using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Health.Views;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using PositionComponent = _Project.Gameplay.Features.Common.Position;

namespace _Project.Gameplay.Features.Health.Systems
{
    public sealed class UpdateHealthBarsSystem : UnitySystemBase
    {
        private static readonly Color BackgroundColor = new(0.08f, 0.09f, 0.1f, 0.9f);
        private static readonly Color FillColor = new(0.2f, 0.85f, 0.38f, 1f);
        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<Health, PositionComponent>();

        private readonly Dictionary<Entity, HealthBarView> _bars = new();
        private readonly HashSet<Entity> _visibleEntities = new();
        private readonly List<Entity> _staleEntities = new();
        private readonly HealthBarConfig _settings;

        private GameObject _documentObject;
        private PanelSettings _panelSettings;
        private VisualElement _root;
        private IPanel _panel;
        private Camera _worldCamera;
        private bool _isUiInitialized;
        private bool _isDisposed;

        public UpdateHealthBarsSystem(World world, HealthBarConfig settings) : base(world)
        {
            _settings = settings;
        }

        public override void Initialize()
        {
            if (_isUiInitialized)
            {
                return;
            }

            _isUiInitialized = true;
            _worldCamera = Camera.main;
            if (_worldCamera == null)
            {
                Debug.LogError("Health bar system requires a camera tagged MainCamera.");
            }

            _panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            _panelSettings.name = "Runtime Health Bar Panel Settings";
            _panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            _panelSettings.sortingOrder = 10f;

            _documentObject = new GameObject("Health Bar UI Document");
            _documentObject.SetActive(false);
            var document = _documentObject.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            document.sortingOrder = 10;
            _documentObject.SetActive(true);

            _root = document.rootVisualElement;
            _root.name = "Health Bars";
            _root.pickingMode = PickingMode.Ignore;
            _root.style.position = UnityEngine.UIElements.Position.Absolute;
            _root.style.left = 0f;
            _root.style.top = 0f;
            _root.style.right = 0f;
            _root.style.bottom = 0f;
            _panel = _root.panel;
            _root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public override void Update(in SystemState state)
        {
            if (_worldCamera == null || _panel == null)
            {
                return;
            }

            _visibleEntities.Clear();
            World.Query(in _description, (Entity entity, ref Health health, ref PositionComponent position) =>
            {
                _visibleEntities.Add(entity);
                if (!_bars.TryGetValue(entity, out var bar))
                {
                    bar = new HealthBarView(
                        _settings.Width,
                        _settings.Height,
                        BackgroundColor,
                        FillColor);
                    _bars.Add(entity, bar);
                    _root.Add(bar);
                }

                var worldPosition =
                    new Vector3(position.Value.x, position.Value.y, 0f) + _settings.WorldOffset;
                var viewportPosition = _worldCamera.WorldToViewportPoint(worldPosition);
                var isVisible = viewportPosition.z > 0f &&
                                viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
                                viewportPosition.y >= 0f && viewportPosition.y <= 1f;

                bar.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!isVisible)
                {
                    return;
                }

                var panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                    _panel,
                    worldPosition,
                    _worldCamera);
                bar.style.left = panelPosition.x - _settings.Width * 0.5f;
                bar.style.top = panelPosition.y - _settings.Height * 0.5f;
                bar.SetFill(health.Max > 0f ? health.Current / health.Max : 0f);
            });

            RemoveStaleBars();
        }

        public override void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _bars.Clear();
            _root?.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            _root?.Clear();

            if (_documentObject != null)
            {
                Object.Destroy(_documentObject);
            }

            if (_panelSettings != null)
            {
                Object.Destroy(_panelSettings);
            }

            base.Dispose();
        }

        private void OnAttachToPanel(AttachToPanelEvent attachEvent)
        {
            _panel = attachEvent.destinationPanel;
        }

        private void RemoveStaleBars()
        {
            _staleEntities.Clear();
            foreach (var pair in _bars)
            {
                if (!_visibleEntities.Contains(pair.Key))
                {
                    _staleEntities.Add(pair.Key);
                }
            }

            foreach (var entity in _staleEntities)
            {
                _bars[entity].RemoveFromHierarchy();
                _bars.Remove(entity);
            }
        }
    }
}
