using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace _Project.Gameplay.Features.Collision
{
    [CreateAssetMenu(
        fileName = "ColliderDebugConfig",
        menuName = "Configs/Gameplay/Collider Debug Config")]
    public sealed class ColliderDebugConfig : SerializedScriptableObject
    {
        [SerializeField, ToggleLeft] private bool enabled = true;
        [SerializeField, Min(1f), LabelText("Scene View Line Width (Pixels)")]
        private float lineWidth = 2f;
        [SerializeField, Min(0.001f), LabelText("Game View Line Width (World Units)")]
        private float gameViewLineWidth = 0.03f;
        [OdinSerialize, DictionaryDrawerSettings(KeyLabel = "Layer", ValueLabel = "Color")]
        private Dictionary<CollisionLayer, Color> colors = CreateDefaultColors();

        public bool Enabled => enabled;
        public float LineWidth => lineWidth;
        public float GameViewLineWidth => gameViewLineWidth;
        public IReadOnlyDictionary<CollisionLayer, Color> Colors => colors;

        public Color GetColor(CollisionLayer layer)
        {
            if (layer == CollisionLayer.None)
            {
                return Color.clear;
            }

            return colors != null && colors.TryGetValue(layer, out var color)
                ? color
                : Color.white;
        }

        private void OnValidate()
        {
            lineWidth = Mathf.Max(1f, lineWidth);
            gameViewLineWidth = Mathf.Max(0.001f, gameViewLineWidth);
        }

        private void OnEnable()
        {
            if (colors == null || colors.Count == 0)
            {
                colors = CreateDefaultColors();
            }
        }

        private static Dictionary<CollisionLayer, Color> CreateDefaultColors()
        {
            return new Dictionary<CollisionLayer, Color>
            {
                [CollisionLayer.Default] = new(1f, 1f, 1f, 0.9f),
                [CollisionLayer.Player] = new(0f, 1f, 1f, 0.9f),
                [CollisionLayer.Enemy] = new(1f, 0f, 0f, 0.9f),
                [CollisionLayer.Environment] = new(0f, 1f, 0f, 0.9f),
                [CollisionLayer.Projectile] = new(1f, 1f, 0f, 0.9f),
                [CollisionLayer.PlayerWeapon] = new(1f, 0f, 1f, 0.9f)
            };
        }
    }
}
