using System;

namespace _Project.Gameplay.Features.Collision
{
    public sealed class CollisionMatrix
    {
        private const int LayerCount = (int)CollisionLayer.Projectile + 1;

        private readonly bool[,] _interactions = new bool[LayerCount, LayerCount];

        public CollisionMatrix()
        {
            for (var first = (int)CollisionLayer.Default; first < LayerCount; first++)
            {
                for (var second = first; second < LayerCount; second++)
                {
                    SetInteraction((CollisionLayer)first, (CollisionLayer)second, true);
                }
            }
        }

        public bool CanInteract(CollisionLayer first, CollisionLayer second)
        {
            return IsValid(first) && IsValid(second) && _interactions[(int)first, (int)second];
        }

        public void SetInteraction(CollisionLayer first, CollisionLayer second, bool enabled)
        {
            if (!IsValid(first))
            {
                throw new ArgumentOutOfRangeException(nameof(first), first, "Choose a concrete collision layer.");
            }

            if (!IsValid(second))
            {
                throw new ArgumentOutOfRangeException(nameof(second), second, "Choose a concrete collision layer.");
            }

            _interactions[(int)first, (int)second] = enabled;
            _interactions[(int)second, (int)first] = enabled;
        }

        private static bool IsValid(CollisionLayer layer)
        {
            return layer > CollisionLayer.None && (int)layer < LayerCount;
        }
    }
}
