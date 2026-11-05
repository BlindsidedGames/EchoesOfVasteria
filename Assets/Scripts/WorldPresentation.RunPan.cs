using System.Collections.Generic;
using TimelessEchoes.UI.Toolkit;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TimelessEchoes
{
    public sealed partial class WorldPresentation
    {
        private readonly List<RaycastResult> panHits = new();
        private GameObject panMap;
        private Vector2 panStart, panPrevious;
        private bool panHeld, panDragging, panRejected;
        private int panPointer;
        private int panReturnFrame = -1;
        private float panOffset, panVelocity;

        internal static void EnableRunPanning() => Get();

        private void OnApplicationFocus(bool focused) { if (!focused) ResetRunPan(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetRunPan(); }

        private bool CanPanRun => active == null && Application.isFocused && ToolkitResponsiveLayout.UserFocusView &&
            !ToolkitDimScreen.BlocksInput && !Characters.CharacterCreation.Required && !ToolkitCharacterCreator.BlocksWorldInput &&
            !ToolkitMeetingScreen.HasVisibleMeeting && GameManager.Instance && !GameManager.Instance.HeroIsDead &&
            !GameManager.Instance.RunLoadingActive && GameManager.Instance.CurrentMap &&
            RunCameraFrame.TryGetCurrent(out var frame) && frame.HasState;

        private void ResetRunPan()
        {
            panHeld = panDragging = panRejected = false;
            panOffset = panVelocity = 0;
            panReturnFrame = -1;
            panMap = null;
        }

        private bool PanOverUI(Vector2 point)
        {
            if (!EventSystem.current) return false;
            panHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, panHits);
            foreach (var hit in panHits)
            {
                if (hit.module is PanelRaycaster toolkit)
                {
                    var screenPoint = new Vector2(point.x, UnityEngine.Device.Screen.height - point.y);
                    if (toolkit.panel != null && toolkit.panel.Pick(RuntimePanelUtils.ScreenToPanel(toolkit.panel, screenPoint)) != null) return true;
                }
                else return true;
            }
            return false;
        }

        private void UpdateRunPan()
        {
            if (!CanPanRun) { ResetRunPan(); return; }
            if (panMap != GameManager.Instance.CurrentMap) { ResetRunPan(); panMap = GameManager.Instance.CurrentMap; }
            var camera = Camera.main;
            if (!camera) { ResetRunPan(); return; }
            var count = 0; var pointer = 0; var position = Vector2.zero;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.isPressed) { count++; pointer = touch.touchId.ReadValue(); position = touch.position.ReadValue(); }
            var down = count > 0;
            if (count == 0 && Mouse.current != null)
            { down = Mouse.current.leftButton.isPressed; position = Mouse.current.position.ReadValue(); }
            if (!down)
            {
                panHeld = panDragging = panRejected = false;
                return;
            }
            if (!panHeld)
            {
                panHeld = true; panPointer = pointer; panStart = panPrevious = position;
                panRejected = count > 1 || !camera.pixelRect.Contains(position) || PanOverUI(position);
            }
            panRejected |= count > 1 || pointer != panPointer || !camera.pixelRect.Contains(position) || PanOverUI(position);
            if (panRejected) return;
            if (!panDragging && Vector2.Distance(panStart, position) < 6) return;
            panDragging = true;
            panOffset -= (position.x - panPrevious.x) * (2 * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight));
            panPrevious = position; panVelocity = 0;
        }

        private void ApplyRunPan(CinemachineBrain brain)
        {
            if (!CanPanRun || !brain || brain.OutputCamera != Camera.main ||
                !RunCameraFrame.TryGetCurrent(out var frame)) return;
            var camera = brain.OutputCamera;
            if (!panHeld && panReturnFrame != Time.frameCount)
            {
                panReturnFrame = Time.frameCount;
                panOffset = Mathf.SmoothDamp(panOffset, 0, ref panVelocity, 1.5f, Mathf.Infinity, Time.unscaledDeltaTime);
                if (Mathf.Abs(panOffset) < .001f) panOffset = panVelocity = 0;
            }

            var origin = camera.transform.position;
            var halfWidth = camera.orthographicSize * camera.aspect;
            var bounds = frame.ActivityBounds;
            var minimum = bounds.min.x + halfWidth;
            if (frame.TryGetComponent<CameraClampExtension>(out var boundary))
                minimum = Mathf.Max(minimum, boundary.MinimumX + halfWidth);
            var visibleOffset = Mathf.Clamp(panOffset, Mathf.Min(0, minimum - origin.x),
                Mathf.Max(0, bounds.max.x - halfWidth - origin.x));
            if (panDragging) panOffset = visibleOffset;
            // During the return a changing boundary can clip the displayed pose
            // without changing the easing state. PixelGridSnap rounds the final pose.
            camera.transform.position = origin + Vector3.right * visibleOffset;
        }
    }
}
