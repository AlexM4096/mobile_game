using PrimeTween;
using UnityEngine;

namespace _Project.Gameplay.Features.Movement.Views
{
    [CreateAssetMenu(
        fileName = "WalkingTweenConfig",
        menuName = "Configs/Gameplay/Walking Tween Config"
    )]
    public sealed class WalkingTweenConfig : ScriptableObject
    {
        [SerializeField] private Vector2 firstScaleMultiplier = new(1.06f, 0.94f);
        [SerializeField] private Vector2 secondScaleMultiplier = new(0.94f, 1.06f);
        [SerializeField, Min(0.01f)] private float stepDuration = 0.16f;
        [SerializeField, Range(0f, 15f)] private float rotationAngle = 3f;
        [SerializeField] private Ease ease = Ease.InOutSine;
        [SerializeField, Range(0f, 1f)] private float noise = 0.65f;

        public Vector2 FirstScaleMultiplier => firstScaleMultiplier;
        public Vector2 SecondScaleMultiplier => secondScaleMultiplier;
        public float StepDuration => stepDuration;
        public float RotationAngle => rotationAngle;
        public Ease Ease => ease;
        public float Noise => noise;
    }
}
