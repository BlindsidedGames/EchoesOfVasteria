using TimelessEchoes.UI.Toolkit;
using Unity.Cinemachine;
using UnityEngine;

namespace TimelessEchoes
{
    /// <summary>The ordinary run view, kept live while presentation uses another camera.</summary>
    [DisallowMultipleComponent]
    public sealed class RunCameraFrame : CinemachineExtension
    {
        private static RunCameraFrame current;
        private CinemachineCamera rig;
        private Camera projection;
        private CameraClampExtension clamp;

        public CameraState State { get; private set; }
        public bool HasState { get; private set; }
        public Bounds VisibleBounds => BoundsForAspect(State.Lens.Aspect);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => current = null;

        public static RunCameraFrame Attach(CinemachineCamera camera)
        {
            if (!camera) return null;
            if (!camera.TryGetComponent(out RunCameraFrame frame)) frame = camera.gameObject.AddComponent<RunCameraFrame>();
            frame.rig = camera;
            frame.TryGetComponent(out frame.clamp);
            camera.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Always;
            if (frame.isActiveAndEnabled) current = frame;
            return frame;
        }

        public static bool TryGetCurrent(out RunCameraFrame frame)
        {
            frame = current;
            if (!frame || !frame.isActiveAndEnabled || !frame.rig || !frame.rig.isActiveAndEnabled)
            { frame = null; return false; }
            var gm = GameManager.Instance;
            if (gm && (!gm.CurrentMap || !frame.transform.IsChildOf(gm.CurrentMap.transform)))
            { frame = null; return false; }
            return true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            TryGetComponent(out rig);
            TryGetComponent(out clamp);
            HasState = false;
            current = this;
        }

        private void OnDisable()
        {
            HasState = false;
            if (current == this) current = null;
        }

        protected override void OnDestroy()
        {
            if (current == this) current = null;
            if (projection) Destroy(projection.gameObject);
            base.OnDestroy();
        }

        // Called explicitly by the normal clamp as well, so component order cannot
        // make its framing math read the cinematic output's aspect for one frame.
        internal static void Prepare(CinemachineVirtualCameraBase camera, ref CameraState state)
        {
            if (!current || current.rig != camera || !current.isActiveAndEnabled) return;
            current.PrepareLens(ref state);
        }

        private void PrepareLens(ref CameraState state)
        {
            var size = new Vector2Int(UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
            if (size.x <= 0 || size.y <= 0) return;
            var pixels = ToolkitResponsiveLayout.WorldPixels(size, UnityEngine.Device.Screen.safeArea, ToolkitResponsiveLayout.UserFocusView);
            if (pixels.width <= 0 || pixels.height <= 0) return;
            if (!projection)
            {
                var owner = new GameObject("Run camera projection") { hideFlags = HideFlags.HideAndDontSave };
                owner.transform.SetParent(transform, false);
                owner.SetActive(false);
                projection = owner.AddComponent<Camera>();
                projection.enabled = false;
                projection.orthographic = true;
            }
            // This component never renders. It supplies the normal view's inherited
            // lens fields through Cinemachine's public API, without reflection.
            projection.aspect = Mathf.Min(pixels.width / pixels.height, 32f / 9f);
            state.Lens.PullInheritedPropertiesFromCamera(projection);
        }

        public override void PrePipelineMutateCameraStateCallback(CinemachineVirtualCameraBase camera, ref CameraState state, float deltaTime)
            => Prepare(camera, ref state);

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase camera, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            // The normal clamp captures after its final correction. A simple rig
            // without that component can still publish its ordinary camera state.
            if (stage == CinemachineCore.Stage.Finalize && (!clamp || !clamp.enabled)) Capture(camera, state);
        }

        internal static void Capture(CinemachineVirtualCameraBase camera, CameraState state)
        {
            if (!TryGetCurrent(out var frame) || frame.rig != camera) return;
            frame.State = state;
            frame.HasState = state.Lens.Orthographic && state.Lens.Aspect > 0 && state.Lens.OrthographicSize > 0;
        }

        internal Bounds ActivityBounds => BoundsForAspect(RunActivityArea.Aspect(State.Lens.Aspect));

        private Bounds BoundsForAspect(float aspect)
        {
            if (!HasState) return default;
            var rotation = State.GetFinalOrientation();
            var horizontal = rotation * Vector3.right * (State.Lens.OrthographicSize * aspect);
            var vertical = rotation * Vector3.up * State.Lens.OrthographicSize;
            var extents = new Vector3(Mathf.Abs(horizontal.x) + Mathf.Abs(vertical.x),
                Mathf.Abs(horizontal.y) + Mathf.Abs(vertical.y), 0);
            var position = State.GetFinalPosition();
            position.z = 0;
            return new Bounds(position, extents * 2);
        }

        public static bool TryGetVisibleBounds(Camera fallback, out Vector3 min, out Vector3 max)
        {
            if (TryGetCurrent(out var frame))
            {
                var bounds = frame.VisibleBounds;
                min = bounds.min; max = bounds.max;
                return frame.HasState;
            }
            min = max = Vector3.zero;
            if (!fallback) return false;
            min = fallback.ViewportToWorldPoint(Vector3.zero);
            max = fallback.ViewportToWorldPoint(Vector3.one);
            return true;
        }
    }
}
