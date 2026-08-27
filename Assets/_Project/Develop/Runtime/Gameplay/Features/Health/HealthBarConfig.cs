using UnityEngine;

namespace _Project.Gameplay.Features.Health
{
    [CreateAssetMenu(fileName = "HealthBarConfig", menuName = "Configs/Gameplay/Health Bar Config")]
    public sealed class HealthBarConfig : ScriptableObject
    {
        [SerializeField] private Vector3 worldOffset = new(0f, 0.45f, 0f);
        [SerializeField, Min(1f)] private float width = 56f;
        [SerializeField, Min(1f)] private float height = 7f;

        public Vector3 WorldOffset => worldOffset;
        public float Width => width;
        public float Height => height;
    }
}