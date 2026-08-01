using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using _Project.Gameplay.Features.Health.Views;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;
using HealthComponent = _Project.Gameplay.Features.Health.Components.Health;
using Object = UnityEngine.Object;
using PositionComponent = _Project.Gameplay.Features.Movement.Components.Position;

namespace _Project.Gameplay.Features.Health.Systems
{
    public readonly struct HealthBarSettings
    {
        public HealthBarSettings(Vector3 worldOffset, float width, float height)
        {
            WorldOffset = worldOffset;
            Width = width;
            Height = height;
        }

        public Vector3 WorldOffset { get; }
        public float Width { get; }
        public float Height { get; }
    }

    public sealed class HealthBarSystem : UnitySystemBase, IInitializable, System.IDisposable
    {
        private static readonly Color BackgroundColor = new(0.08f, 0.09f, 0.1f, 0.9f);
        private static readonly Color FillColor = new(0.2f, 0.85f, 0.38f, 1f);

        private readonly QueryDescription query =
            new QueryDescription()
                .WithAll<HealthComponent, PositionComponent>();

        private readonly Dictionary<Entity, HealthBarView> bars = new();
        private readonly HashSet<Entity> visibleEntities = new();
        private readonly List<Entity> staleEntities = new();
        private readonly HealthBarSettings settings;

        private GameObject documentObject;
        private PanelSettings panelSettings;
        private VisualElement root;
        private IPanel panel;
        private Camera worldCamera;
        private bool isUiInitialized;
        private bool isDisposed;

        public HealthBarSystem(World world, HealthBarSettings settings) : base(world)
        {
            this.settings = settings;
        }

        void IInitializable.Initialize()
        {
            if (isUiInitialized)
            {
                return;
            }

            isUiInitialized = true;
            worldCamera = Camera.main;
            if (worldCamera == null)
            {
                Debug.LogError("Health bar system requires a camera tagged MainCamera.");
            }

            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Runtime Health Bar Panel Settings";
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.sortingOrder = 10f;

            documentObject = new GameObject("Health Bar UI Document");
            documentObject.SetActive(false);
            var document = documentObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = 10;
            documentObject.SetActive(true);

            root = document.rootVisualElement;
            root.name = "Health Bars";
            root.pickingMode = PickingMode.Ignore;
            root.style.position = UnityEngine.UIElements.Position.Absolute;
            root.style.left = 0f;
            root.style.top = 0f;
            root.style.right = 0f;
            root.style.bottom = 0f;
            panel = root.panel;
            root.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public override void Update(in SystemState state)
        {
            if (worldCamera == null || panel == null)
            {
                return;
            }

            visibleEntities.Clear();
            World.Query(in query, (
                Entity entity,
                ref HealthComponent health,
                ref PositionComponent position
            ) =>
            {
                visibleEntities.Add(entity);
                if (!bars.TryGetValue(entity, out var bar))
                {
                    bar = new HealthBarView(settings.Width, settings.Height, BackgroundColor, FillColor);
                    bars.Add(entity, bar);
                    root.Add(bar);
                }

                var worldPosition = new Vector3(position.Value.x, position.Value.y, 0f) + settings.WorldOffset;
                var viewportPosition = worldCamera.WorldToViewportPoint(worldPosition);
                var isVisible = viewportPosition.z > 0f &&
                                viewportPosition.x >= 0f && viewportPosition.x <= 1f &&
                                viewportPosition.y >= 0f && viewportPosition.y <= 1f;

                bar.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!isVisible)
                {
                    return;
                }

                var panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                    panel,
                    worldPosition,
                    worldCamera);
                bar.style.left = panelPosition.x - settings.Width * 0.5f;
                bar.style.top = panelPosition.y - settings.Height * 0.5f;
                bar.SetFill(health.Normalized);
            });

            RemoveStaleBars();
        }

        public override void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            bars.Clear();
            root?.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            root?.Clear();

            if (documentObject != null)
            {
                Object.Destroy(documentObject);
            }

            if (panelSettings != null)
            {
                Object.Destroy(panelSettings);
            }

            base.Dispose();
        }

        private void OnAttachToPanel(AttachToPanelEvent attachEvent)
        {
            panel = attachEvent.destinationPanel;
        }

        private void RemoveStaleBars()
        {
            staleEntities.Clear();
            foreach (var pair in bars)
            {
                if (!visibleEntities.Contains(pair.Key))
                {
                    staleEntities.Add(pair.Key);
                }
            }

            for (var index = 0; index < staleEntities.Count; index++)
            {
                var entity = staleEntities[index];
                bars[entity].RemoveFromHierarchy();
                bars.Remove(entity);
            }
        }
    }
}
