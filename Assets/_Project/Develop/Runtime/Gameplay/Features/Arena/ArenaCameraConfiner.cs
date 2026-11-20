using Unity.Cinemachine;
using UnityEngine;

namespace _Project.Gameplay.Features.Arena
{
    public sealed class ArenaCameraConfiner : CinemachineExtension
    {
        public Rect Bounds { get; set; }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || !state.Lens.Orthographic)
            {
                return;
            }

            var lens = state.Lens;
            var aspect = Mathf.Max(0.01f, lens.Aspect);
            lens.OrthographicSize = Mathf.Min(lens.OrthographicSize,
                Mathf.Min(Bounds.height * 0.5f, Bounds.width / (2f * aspect)));
            state.Lens = lens;

            var center = state.GetFinalPosition();
            var halfHeight = lens.OrthographicSize;
            var halfWidth = halfHeight * aspect;
            var clampedX = Mathf.Clamp(center.x, Bounds.xMin + halfWidth, Bounds.xMax - halfWidth);
            var clampedY = Mathf.Clamp(center.y, Bounds.yMin + halfHeight, Bounds.yMax - halfHeight);
            state.PositionCorrection += new Vector3(clampedX - center.x, clampedY - center.y, 0f);
        }
    }
}
