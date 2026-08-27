using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    public readonly struct PlayerOrbitVisualizationSettings
    {
        public PlayerOrbitVisualizationSettings(Color color, float lineWidth, float markerRadius)
        {
            Color = color;
            LineWidth = Mathf.Max(0.001f, lineWidth);
            MarkerRadius = Mathf.Max(0.001f, markerRadius);
        }

        public Color Color { get; }
        public float LineWidth { get; }
        public float MarkerRadius { get; }
    }
}
