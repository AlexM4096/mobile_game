using UnityEngine;

namespace _Project.Gameplay.Features.Player
{
    [CreateAssetMenu(fileName = "PlayerOrbitVisualizationConfig", menuName = "Configs/Gameplay/Player Orbit Visualization Config")]
    public sealed class PlayerOrbitVisualizationConfig : ScriptableObject
    {
        [SerializeField] private Color color = new(0f, 1f, 1f, 0.75f);
        [SerializeField, Min(0.001f)] private float lineWidth = 0.05f;
        [SerializeField, Min(0.001f)] private float markerRadius = 0.15f;

        public Color Color => color;
        public float LineWidth => lineWidth;
        public float MarkerRadius => markerRadius;

        private void OnValidate()
        {
            lineWidth = Mathf.Max(0.001f, lineWidth);
            markerRadius = Mathf.Max(0.001f, markerRadius);
        }
    }
}