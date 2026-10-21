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

        private sealed class ComposerState : VcamExtraStateBase
        {
            public CinemachinePositionComposer Composer;
            public ScreenComposerSettings AuthoredComposition;
            public float AuthoredLookaheadTime;
            public bool Adjusted;
            public ComposerState() { }
        }

        public override void PrePipelineMutateCameraStateCallback(
            CinemachineVirtualCameraBase vcam, ref CameraState state, float deltaTime)
        {
            var extra = GetExtraState<ComposerState>(vcam);
            if (!extra.Composer && !vcam.TryGetComponent(out extra.Composer)) return;
            if (extra.Adjusted)
            {
                extra.Composer.Composition = extra.AuthoredComposition;
                extra.Composer.Lookahead.Time = extra.AuthoredLookaheadTime;
            }
            extra.Adjusted = false;

            var displayAspect = state.Lens.Aspect;
            var activityAspect = RunActivityArea.Aspect(displayAspect);
            var portrait = Application.isPlaying && UI.Toolkit.ToolkitResponsiveLayout.IsPortrait && !UI.Toolkit.ToolkitResponsiveLayout.TownView;
            if (!portrait || !state.Lens.Orthographic || displayAspect <= 0f) return;

            // A screen-space dead zone would shrink in world units when the view
            // is cropped. Preserve the authored landscape tracking region while
            // the composer runs, then restore its serialized settings below.
            extra.AuthoredComposition = extra.Composer.Composition;
            extra.AuthoredLookaheadTime = extra.Composer.Lookahead.Time;
            // A full second of prediction can put a fast player outside a narrow
            // crop. Keep some lead, scaled down with the visible horizontal span.
            extra.Composer.Lookahead.Time *= .35f * Mathf.Min(1f, displayAspect / RunActivityArea.PortraitAspect);
            var composition = extra.AuthoredComposition;
            var widthScale = activityAspect / displayAspect;
            composition.DeadZone.Size.x *= widthScale;
            composition.HardLimits.Size.x *= widthScale;
            extra.Composer.Composition = composition;
            extra.Adjusted = true;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Finalize)
            {
                var extra = GetExtraState<ComposerState>(vcam);
                if (extra.Adjusted && extra.Composer)
                {
                    extra.Composer.Composition = extra.AuthoredComposition;
                    extra.Composer.Lookahead.Time = extra.AuthoredLookaheadTime;
                    extra.Adjusted = false;
                }
                var pos = state.GetFinalPosition();
                pos.y = yLevel;
                float halfWidth = state.Lens.OrthographicSize * RunActivityArea.Aspect(state.Lens.Aspect);
                pos.x = Mathf.Max(minX + halfWidth, pos.x);
                if (Application.isPlaying && !UI.Toolkit.ToolkitResponsiveLayout.TownView &&
                    state.Lens.Orthographic && vcam.Follow != null)
                {
                    // Bound the actual lead too: arbitrarily high movement speed
                    // must not place the tracked player beyond the visible crop.
                    var margin = UI.Toolkit.ToolkitResponsiveLayout.IsPortrait ? .65f : .9f;
                    var maximumLead = state.Lens.OrthographicSize * state.Lens.Aspect * margin;
                    pos.x = Mathf.Clamp(pos.x, vcam.Follow.position.x - maximumLead, vcam.Follow.position.x + maximumLead);
                }
                state.PositionCorrection += pos - state.GetFinalPosition();
            }
        }
    }
}
