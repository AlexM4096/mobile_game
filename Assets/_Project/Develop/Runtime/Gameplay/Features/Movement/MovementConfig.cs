using UnityEngine;

namespace _Project.Gameplay.Features.Movement
{
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "Configs/Gameplay/Movement Config")]
    public sealed class MovementConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float arrivalDistance = 0.05f;

        public float ArrivalDistance => arrivalDistance;
    }
}