using UnityEngine;

namespace _Project.Gameplay.Features.Arena
{
    [CreateAssetMenu(fileName = "ArenaBoundsConfig", menuName = "Configs/Gameplay/Arena Bounds Config")]
    public sealed class ArenaBoundsConfig : ScriptableObject
    {
        [SerializeField] private Vector2 center = new(0f, -4.25f);
        [SerializeField, Min(1f)] private float horizontalHalfSize = 260f;
        [SerializeField, Min(1f)] private float verticalHalfSize = 64f;
        [SerializeField, Min(0.1f)] private float wallThickness = 2f;

        public Vector2 Center => center;
        public float HorizontalHalfSize => horizontalHalfSize;
        public float VerticalHalfSize => verticalHalfSize;
        public float WallThickness => wallThickness;

        public bool TryValidate(out string error)
        {
            if (!IsPositiveFinite(horizontalHalfSize))
            {
                error = "Arena horizontal half size must be a positive finite value.";
                return false;
            }

            if (!IsFinite(center.x) || !IsFinite(center.y))
            {
                error = "Arena center must contain finite values.";
                return false;
            }

            if (!IsPositiveFinite(verticalHalfSize))
            {
                error = "Arena vertical half size must be a positive finite value.";
                return false;
            }

            if (!IsPositiveFinite(wallThickness))
            {
                error = "Arena wall thickness must be a positive finite value.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && IsFinite(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
