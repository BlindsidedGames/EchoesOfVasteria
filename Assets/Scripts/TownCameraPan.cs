using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TimelessEchoes
{
    /// <summary>Pans and zooms the town camera inside authored land bounds.</summary>
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineConfiner2D))]
    public sealed class TownCameraPan : MonoBehaviour
    {
        [SerializeField] private Transform panTarget;
        [SerializeField] private BoxCollider2D landBounds;
        [SerializeField] private Camera outputCamera;
        [SerializeField, Min(1)] private float preferredSize = 18f;
        [SerializeField, Min(0)] private float dragThreshold = 6f;

        private CinemachineCamera townCamera;
        private CinemachineConfiner2D confiner;
        private readonly List<RaycastResult> hits = new();
        private Vector2 previous, start;
        private bool held, dragging, rejected;
        private int pointerId = int.MinValue;
        private float lastAspect;
        private float requestedSize;
        private float pinchDistance;
        private int pinchIdA, pinchIdB;
        private bool pinching, pinchRejected;
        private InputAction moveAction;

        private void Awake()
        {
            townCamera = GetComponent<CinemachineCamera>();
            confiner = GetComponent<CinemachineConfiner2D>();
            requestedSize = preferredSize;
            moveAction = InputSystem.actions?.FindAction("Player/Move");
        }

        private void OnDisable() => ResetGesture();
        private void OnApplicationFocus(bool focused) { if (!focused) ResetGesture(); }

        private void ResetGesture()
        {
            held = dragging = rejected = false;
            pointerId = int.MinValue;
            pinching = pinchRejected = false;
        }

        private bool OverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = position }, hits);
            return hits.Count != 0;
        }

        private bool CanZoomAt(Vector2 position) =>
            outputCamera.pixelRect.Contains(position) && !OverUI(position);

        private void Update()
        {
            if (panTarget == null || landBounds == null || outputCamera == null) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsInTown)
            {
                ResetGesture();
                return;
            }

            // Use the rendered viewport, including AspectRatioBox's letterboxing.
            float aspect = (float)outputCamera.pixelWidth / Mathf.Max(1, outputCamera.pixelHeight);
            var bounds = landBounds.bounds;
            // Leave one native pixel of slack for the final PixelGridSnap adjustment.
            const float slack = 1f / 16f;
            float maxSize = Mathf.Min(preferredSize * 1.5f, bounds.extents.y - slack,
                (bounds.extents.x - slack) / aspect);
            float minSize = Mathf.Min(preferredSize * .5f, maxSize);
            requestedSize = Mathf.Clamp(requestedSize, minSize, maxSize);

            Vector2 position = default, secondPosition = default;
            bool down = false;
            int id = -1, secondId = -1, touches = 0;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.isPressed)
                    {
                        touches++;
                        if (touches == 1)
                        {
                            position = touch.position.ReadValue();
                            id = touch.touchId.ReadValue();
                        }
                        else if (touches == 2)
                        {
                            secondPosition = touch.position.ReadValue();
                            secondId = touch.touchId.ReadValue();
                        }
                    }

            if (touches >= 2)
            {
                float distance = Vector2.Distance(position, secondPosition);
                bool blocked = touches != 2 || !CanZoomAt(position) || !CanZoomAt(secondPosition);
                if (!pinching)
                {
                    pinchRejected = rejected || blocked;
                    pinchIdA = id;
                    pinchIdB = secondId;
                    pinching = true;
                }
                else
                {
                    pinchRejected |= blocked || id != pinchIdA || secondId != pinchIdB;
                    if (!pinchRejected && distance > 1f && pinchDistance > 1f)
                        requestedSize *= pinchDistance / distance;
                }
                pinchDistance = distance;
                // Lifting one finger must not turn the remaining finger into a drag.
                held = rejected = true;
            }
            else if (touches == 0 && Mouse.current != null && CanZoomAt(Mouse.current.position.ReadValue()))
            {
                // The project's UniformAcrossAllPlatforms setting reports 1 per notch.
                float scroll = Mouse.current.scroll.ReadValue().y;
                requestedSize *= Mathf.Exp(-Mathf.Clamp(scroll, -20f, 20f) * .15f);
            }
            float size = requestedSize = Mathf.Clamp(requestedSize, minSize, maxSize);
            if (!Mathf.Approximately(size, townCamera.Lens.OrthographicSize) || !Mathf.Approximately(aspect, lastAspect))
            {
                townCamera.Lens.OrthographicSize = size;
                confiner.InvalidateLensCache();
                lastAspect = aspect;
            }

            Vector3 Clamp(Vector3 p)
            {
                float halfWidth = size * aspect + slack, halfHeight = size + slack;
                p.x = Mathf.Clamp(p.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
                p.y = Mathf.Clamp(p.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight);
                return p;
            }
            panTarget.position = Clamp(panTarget.position);

            // Read the project's shared horizontal/vertical movement action.
            // Scale speed with zoom so crossing the screen takes a consistent time.
            if (moveAction != null && Application.isFocused &&
                (UI.TownWindowManager.Instance == null || !UI.TownWindowManager.Instance.HasOpenWindow))
            {
                Vector2 movement = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
                panTarget.position = Clamp(panTarget.position +
                    new Vector3(movement.x, movement.y, 0) * (size * 1.2f * Time.unscaledDeltaTime));
            }

            if (touches > 1) return;
            if (touches == 1) down = true;
            else if (Mouse.current != null)
            {
                down = Mouse.current.leftButton.isPressed || Mouse.current.middleButton.isPressed;
                position = Mouse.current.position.ReadValue();
            }
            if (!down) { ResetGesture(); return; }
            if (!held)
            {
                held = true;
                pointerId = id;
                start = previous = position;
                rejected = !outputCamera.pixelRect.Contains(position) || OverUI(position);
                return;
            }
            if (id != pointerId) rejected = true;
            if (rejected) return;
            if (!outputCamera.pixelRect.Contains(position) || OverUI(position))
            {
                rejected = true;
                return;
            }
            if (!dragging && Vector2.Distance(start, position) < dragThreshold) return;
            dragging = true;
            Vector2 delta = position - previous;
            previous = position;
            float unitsPerPixel = 2f * size / Mathf.Max(1, outputCamera.pixelHeight);
            panTarget.position = Clamp(panTarget.position - new Vector3(delta.x, delta.y, 0) * unitsPerPixel);
        }
    }
}
