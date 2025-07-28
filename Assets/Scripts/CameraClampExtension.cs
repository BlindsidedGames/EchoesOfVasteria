using Unity.Cinemachine;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Cinemachine extension that clamps the camera to a fixed Y level and
    /// ensures the left edge of the view never goes left of <c>minX</c>.
    /// Attach this component to a CinemachineCamera.
    /// </summary>
    [SaveDuringPlay]
    [AddComponentMenu("")]
    public class CameraClampExtension : CinemachineExtension
    {
        [SerializeField] private float yLevel = 0f;
        [SerializeField] private float minX = 0f;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize)
            {
                var pos = state.GetFinalPosition();
                pos.y = yLevel;
                float halfWidth = state.Lens.OrthographicSize * state.Lens.Aspect;
                pos.x = Mathf.Max(minX + halfWidth, pos.x);
                state.PositionCorrection += pos - state.GetFinalPosition();
            }
        }
    }
}
