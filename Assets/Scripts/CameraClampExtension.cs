using Cinemachine;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>
    /// Cinemachine extension that clamps the camera to a fixed Y level and
    /// prevents the X position from going below a minimum value.
    /// Attach this component to a CinemachineVirtualCamera.
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
            if (stage == CinemachineCore.Stage.Body)
            {
                var pos = state.RawPosition;
                pos.y = yLevel;
                pos.x = Mathf.Max(minX, pos.x);
                state.RawPosition = pos;
            }
        }
    }
}
